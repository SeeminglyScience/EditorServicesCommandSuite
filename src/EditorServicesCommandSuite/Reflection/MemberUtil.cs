using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using EditorServicesCommandSuite.Inference;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;
using Microsoft.PowerShell;

namespace EditorServicesCommandSuite.Reflection
{
    internal static class MemberUtil
    {
        private const BindingFlags AllMembers =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;

        private static readonly ConcurrentDictionary<(Type type, bool abstractOnly), ImmutableArray<MemberDescription>> s_abstractMembersCache =
            new ConcurrentDictionary<(Type type, bool abstractOnly), ImmutableArray<MemberDescription>>();

        private enum UnimplementableMemberAction
        {
            None,

            Skip,

            Throw,
        }

        public static BindingFlags GetBindingFlags(MemberInfo member)
        {
            return member switch
            {
                MethodBase method => GetBindingFlags(method.IsPublic, method.IsStatic),
                FieldInfo field => GetBindingFlags(field.IsPublic, field.IsStatic),
                PropertyInfo property => GetBindingFlags(property.GetGetMethod(nonPublic: true)),
                EventInfo eventInfo => GetBindingFlags(eventInfo.GetAddMethod(nonPublic: true)),
                Type type => GetBindingFlags(type.IsNestedPublic, isStatic: true),
                _ => default,
            };
        }

        public static BindingFlags GetBindingFlags(bool isPublic, bool isStatic, bool ignoreCase = false)
        {
            var flags = isPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            flags |= isStatic ? BindingFlags.Static : BindingFlags.Instance;
            return ignoreCase ? flags | BindingFlags.IgnoreCase : flags;
        }

        public static bool IsTypeImplementable(Type type)
        {
            return !type.GetMethods(AllMembers).Any(FilterIsNotImplementableRequired);
        }

        public static bool IsTypeExpressible(Type type)
        {
            return IsTypeExpressible(type, shouldSkipGenericArgs: false);
        }

        public static bool IsTypeExpressible(Type type, bool shouldSkipGenericArgs)
        {
            if (!IsTypeVisible(type))
            {
                return false;
            }

            if (type.IsPointer || type.IsByRef || type.IsGenericParameter)
            {
                return false;
            }

            if (!shouldSkipGenericArgs && type.IsGenericTypeDefinition)
            {
                return false;
            }

            if (shouldSkipGenericArgs || !type.IsConstructedGenericType)
            {
                return true;
            }

            return !type.ContainsGenericParameters
                && type.GetGenericArguments().All(IsTypeExpressible);
        }

        public static ImmutableArray<MemberDescription> GetImplementableMethods(Type subject)
        {
            return FindMembers(
                subject,
                MemberTypes.Method,
                FilterIsImplementable,
                null);
        }

        public static ImmutableArray<MemberDescription> GetVirtualMethods(Type subject, bool abstractOnly = true)
        {
            return s_abstractMembersCache.GetOrAdd(
                (subject, abstractOnly),
                GetVirtualMethodsImpl);
        }

        internal static bool TryGetResolvedType(string name, out Type type)
        {
            type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetModules())
                .SelectMany(module =>
                {
                    try
                    {
                        return module.FindTypes(
                            Module.FilterTypeNameIgnoreCase,
                            name);
                    }
                    catch
                    {
                        return Enumerable.Empty<Type>();
                    }
                })
                .FirstOrDefault();

            return type != null;
        }

        internal static Type[] ResolveTypes(string name, bool searchFullName = false)
        {
            ReadOnlySpan<char> nameAsSpan = name.AsSpan();
            List<Type> foundTypes = null;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Module module in assembly.GetModules())
                {
                    try
                    {
                        foreach (Type type in module.GetTypes())
                        {
                            if (!type.IsPublic)
                            {
                                continue;
                            }

                            if (type.IsGenericType)
                            {
                                ReadOnlySpan<char> genericName = (searchFullName ? type.FullName : type.Name).AsSpan();
                                int backTick = genericName.LastIndexOf(Symbols.Backtick);
                                if (backTick != -1)
                                {
                                    genericName = genericName.Slice(0, backTick);
                                }

                                if (genericName.Equals(nameAsSpan, StringComparison.OrdinalIgnoreCase))
                                {
                                    (foundTypes ??= new List<Type>()).Add(type);
                                }

                                continue;
                            }

                            string nameToSearch = searchFullName ? type.FullName : type.Name;
                            if (nameToSearch.Equals(name, StringComparison.OrdinalIgnoreCase))
                            {
                                (foundTypes ??= new List<Type>()).Add(type);
                            }
                        }
                    }
                    catch
                    {
                        // Skip any types that throw on type load.
                    }
                }
            }

            return foundTypes?.ToArray() ?? Array.Empty<Type>();
        }

        internal static string GetTypeNameForLiteral(Type type)
        {
            return GetTypeNameForLiteral(type, false, out _);
        }

        internal static string GetTypeNameForLiteral(
            Type type,
            bool dropNamespaces,
            out string[] droppedNamespaces,
            bool forAttribute = false,
            bool skipGenericArgs = false)
        {
            var dropped = new HashSet<string>();
            var builder = new StringBuilder();
            GetTypeNameForLiteralImpl(
                type,
                dropNamespaces,
                dropped,
                builder,
                skipGenericArgs);

            droppedNamespaces = dropped.ToArray();
            if (forAttribute)
            {
                return GetTypeNameForAttribute(builder.ToString());
            }

            return builder.ToString();
        }

        internal static IEnumerable<MemberInfo> GetMembers(Type type, bool isStatic = false, bool includeNonPublic = false)
        {
            var flags = BindingFlags.Public;
            if (includeNonPublic)
            {
                flags |= BindingFlags.NonPublic;
            }

            flags |= isStatic ? BindingFlags.Static : BindingFlags.Instance;
            return type.GetMembers(flags);
        }

        internal static IEnumerable<PSMemberInfo> GetPSMembers(
            Type type,
            bool isStatic = false,
            bool includeNonPublic = false)
        {
            var flags = BindingFlags.Public;
            flags |= isStatic ? BindingFlags.Static : BindingFlags.Instance;
            if (includeNonPublic)
            {
                flags |= BindingFlags.NonPublic;
            }

            foreach (MemberInfo member in type.GetMembers(flags))
            {
                if (member is MethodInfo method)
                {
                    yield return new ReflectionMethodInfo(method);
                    continue;
                }

                if (member is ConstructorInfo constructor)
                {
                    yield return new ReflectionMethodInfo(constructor);
                    continue;
                }

                if (member is PropertyInfo property)
                {
                    yield return new ReflectionPropertyInfo(property);
                    continue;
                }

                if (member is FieldInfo field)
                {
                    yield return new ReflectionFieldInfo(field);
                    continue;
                }
            }
        }

        internal static bool IsTypeVisible(Type type)
        {
            if (type.IsPublic)
            {
                return true;
            }

            if (!type.IsNestedPublic)
            {
                return false;
            }

            return IsTypeVisible(type.ReflectedType);
        }

        internal static string GetShortestExpressibleTypeName(Type type, out string droppedNamespace)
        {
            var assembly = type.Assembly;
            if (ReflectionCache.TypeAccelerators_Get?.GetValue(null) is Dictionary<string, Type> accelerators)
            {
                string foundAccelerator = accelerators
                    .Where(pair => pair.Value.Assembly == assembly)
                    .OrderBy(pair => pair.Key.Length)
                    .FirstOrDefault()
                    .Key;

                if (!string.IsNullOrEmpty(foundAccelerator))
                {
                    droppedNamespace = null;
                    return foundAccelerator;
                }
            }

            if (Settings.EnableAutomaticNamespaceRemoval)
            {
                Type shortestName = assembly.GetTypes()
                    .Where(t => IsTypeExpressible(t) && t.IsPublic)
                    .OrderBy(t => t.Name.Length)
                    .FirstOrDefault();

                droppedNamespace = shortestName?.Namespace;
                return shortestName.Name;
            }

            droppedNamespace = null;
            return assembly.GetTypes()
                .Where(t => IsTypeExpressible(t) && t.IsPublic)
                .OrderBy(t => t.FullName.Length)
                .FirstOrDefault()
                ?.FullName;
        }

        internal static void GetTypeNameForLiteralImpl(
            Type type,
            bool dropNamespaces,
            HashSet<string> droppedNamespaces,
            StringBuilder builder,
            bool skipGenericArgs)
        {
            if (!type.IsGenericType)
            {
                var byEngine = ToStringCodeMethods.Type(new PSObject(type));
                if (!byEngine.Equals(type.FullName, StringComparison.Ordinal))
                {
                    builder.Append(byEngine);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(type.Namespace))
            {
                if (dropNamespaces)
                {
                        droppedNamespaces.Add(type.Namespace);
                }
                else
                {
                    builder.Append(type.Namespace).Append(Symbols.Dot);
                }
            }

            builder.Append(type.Name.Split(Symbols.Backtick)[0]);
            if (!type.IsGenericType || skipGenericArgs)
            {
                return;
            }

            builder.Append(Symbols.SquareOpen);
            var genericArgs = type.GetGenericArguments();
            for (var i = 0; i < genericArgs.Length; i++)
            {
                GetTypeNameForLiteralImpl(
                    genericArgs[i],
                    dropNamespaces,
                    droppedNamespaces,
                    builder,
                    skipGenericArgs);

                if (i < genericArgs.Length - 1)
                {
                    builder.Append(Symbols.Comma).Append(Symbols.Space);
                }
            }

            builder.Append(Symbols.SquareClose);
        }

        private static ImmutableArray<MemberDescription> GetVirtualMethodsImpl((Type subject, bool abstractOnly) args)
        {
            var (subject, abstractOnly) = args;
            return FindMembers(
                subject,
                MemberTypes.Method,
                Type.FilterAttribute,
                abstractOnly ? MethodAttributes.Abstract : MethodAttributes.Virtual,
                abstractOnly ? UnimplementableMemberAction.Throw : UnimplementableMemberAction.Skip);
        }

        private static ImmutableArray<MemberDescription> FindMembers(
            Type subject,
            MemberTypes memberTypes,
            MemberFilter filter,
            object criteria = null,
            UnimplementableMemberAction unimplementableAction = UnimplementableMemberAction.None)
        {
            MemberFilter aggregateFilter = unimplementableAction switch
            {
                UnimplementableMemberAction.None => filter,
                UnimplementableMemberAction.Skip => (m, c) => filter(m, c) && FilterIsImplementable(m, c),
                UnimplementableMemberAction.Throw => (m, c) =>
                {
                    if (!filter(m, c))
                    {
                        return false;
                    }

                    if (!FilterIsImplementable(m, c))
                    {
                        throw Error.InvalidTypeForPowerShellBase(subject.FullName);
                    }

                    return true;
                },
                _ => throw new PSArgumentOutOfRangeException(nameof(unimplementableAction))
            };

            if (subject.IsInterface)
            {
                return FindInterfaceMembers(subject, memberTypes, aggregateFilter, criteria);
            }

            var members = subject.FindMembers(memberTypes, AllMembers, aggregateFilter, criteria);
            var result = ImmutableArray.CreateBuilder<MemberDescription>(members.Length);
            foreach (MemberInfo member in members)
            {
                result.Add(new ReflectedMemberDescription(member));
            }

            return result.MoveToImmutable();
        }

        private static ImmutableArray<MemberDescription> FindInterfaceMembers(
            Type subject,
            MemberTypes memberTypes,
            MemberFilter filter,
            object criteria)
        {
            var members = new List<MemberDescription>();
            var typesToProcess = new Stack<Type>();
            typesToProcess.Push(subject);
            while (typesToProcess.Count > 0)
            {
                var current = typesToProcess.Pop();
                Type[] nestedInterfaces = current.GetInterfaces();
                foreach (Type nestedInterface in nestedInterfaces)
                {
                    typesToProcess.Push(nestedInterface);
                }

                members.AddRange(
                    current
                        .FindMembers(
                            memberTypes,
                            AllMembers,
                            filter,
                            criteria)
                        .ToMemberDescriptions());
            }

            return members.ToImmutableArray();
        }

        private static bool FilterIsImplementable(MemberInfo m, object criteria)
        {
            if (!(m is MethodInfo method))
            {
                return false;
            }

            MethodAttributes access = method.Attributes & MethodAttributes.MemberAccessMask;
            bool isAccessible = access == MethodAttributes.Public ||
                access == MethodAttributes.Family ||
                access == MethodAttributes.FamORAssem;

            if (!isAccessible || method.IsGenericMethod || !IsTypeExpressible(method.ReturnType))
            {
                return false;
            }

            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (!IsTypeExpressible(parameter.ParameterType))
                {
                    return false;
                }
            }

            // Finalizers probably shouldn't be implemented in PowerShell.
            bool isObjectFinalize = method.Name.Equals("Finalize", StringComparison.Ordinal)
                && method.DeclaringType == typeof(object);

            return !isObjectFinalize;
        }

        private static bool FilterIsNotImplementableRequired(MemberInfo m)
        {
            return m is MethodInfo method &&
                method.IsAbstract &&
                !FilterIsImplementable(m, null);
        }

        private static string GetTypeNameForAttribute(string typeName)
        {
            if (!typeName.EndsWith(Symbols.Attribute, StringComparison.OrdinalIgnoreCase))
            {
                return typeName;
            }

            return typeName.Substring(0, typeName.Length - Symbols.Attribute.Length);
        }
    }
}
