using System;
using System.Collections.Generic;
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
        private static readonly BindingFlags s_allMembers =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static;

        public static BindingFlags GetBindingFlags(MemberInfo member)
        {
            if (member is MethodBase method)
            {
                return GetBindingFlags(method.IsPublic, method.IsStatic);
            }

            if (member is FieldInfo field)
            {
                return GetBindingFlags(field.IsPublic, field.IsStatic);
            }

            if (member is PropertyInfo property)
            {
                return GetBindingFlags(property.GetGetMethod(nonPublic: true));
            }

            if (member is EventInfo eventInfo)
            {
                return GetBindingFlags(eventInfo.GetAddMethod(nonPublic: true));
            }

            if (member is Type type)
            {
                return GetBindingFlags(type.IsNestedPublic, isStatic: true);
            }

            return default(BindingFlags);
        }

        public static BindingFlags GetBindingFlags(bool isPublic, bool isStatic, bool ignoreCase = false)
        {
            var flags = isPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            flags |= isStatic ? BindingFlags.Static : BindingFlags.Instance;
            return ignoreCase ? flags | BindingFlags.IgnoreCase : flags;
        }

        public static bool IsTypeImplementable(Type type)
        {
            return !type.GetMethods(s_allMembers).Any(member => FilterIsNotImplementableRequired(member, null));
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

        public static IEnumerable<MemberDescription> GetImplementableMethods(Type subject)
        {
            return subject.FindMembers(
                MemberTypes.Method,
                s_allMembers,
                FilterIsImplementable,
                null)
                .ToMemberDescriptions();
        }

        public static IEnumerable<MemberDescription> GetAbstractMethods(Type subject)
        {
            return subject.FindMembers(
                MemberTypes.Method,
                s_allMembers,
                Type.FilterAttribute,
                MethodAttributes.Abstract)
                .ToMemberDescriptions();
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
            var accelerators = ReflectionCache.TypeAccelerators_Get?.GetValue(null) as Dictionary<string, Type>;
            if (accelerators != null)
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
            if (skipGenericArgs || !type.IsGenericType)
            {
                var byEngine = ToStringCodeMethods.Type(new PSObject(type));
                if (!byEngine.Equals(type.FullName, StringComparison.Ordinal))
                {
                    builder.Append(byEngine);
                    return;
                }
            }

            if (dropNamespaces && !string.IsNullOrEmpty(type.Namespace))
            {
                droppedNamespaces.Add(type.Namespace);
            }
            else
            {
                builder.Append(type.Namespace).Append(Symbols.Dot);
            }

            if (skipGenericArgs)
            {
                builder.Append(type.Name);
                return;
            }

            builder.Append(type.Name.Split(Symbols.Backtick)[0]);
            if (!type.IsGenericType)
            {
                return;
            }

            builder.Append(Symbols.SquareOpen);
            var genericArgs = type.GetGenericArguments();
            for (var i = 0; i < genericArgs.Length; i++)
            {
                GetTypeNameForLiteralImpl(
                    type,
                    dropNamespaces,
                    droppedNamespaces,
                    builder,
                    skipGenericArgs);

                if (i < genericArgs.Length - 1)
                {
                    builder.Append(Symbols.Comma + Symbols.Space);
                }
            }

            builder.Append(Symbols.SquareClose);
        }

        private static bool FilterIsImplementable(MemberInfo m, object criteria)
        {
            return m is MethodInfo method &&
                (method.Attributes.HasFlag(MethodAttributes.Public) ||
                method.Attributes.HasFlag(MethodAttributes.Family) ||
                method.Attributes.HasFlag(MethodAttributes.FamORAssem)) &&
                !method.IsGenericMethod &&
                IsTypeExpressible(method.ReturnType) &&
                method.GetParameters().All(p => IsTypeExpressible(p.ParameterType));
        }

        private static bool FilterIsNotImplementableRequired(MemberInfo m, object criteria)
        {
            return m is MethodInfo method &&
                method.IsAbstract &&
                !FilterIsImplementable(m, criteria);
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
