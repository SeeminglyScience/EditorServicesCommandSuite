using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
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

        public static bool IsTypeImplementable(Type type)
        {
            return !type.GetMethods(s_allMembers).Any(member => FilterIsNotImplementableRequired(member, null));
        }

        public static bool IsTypeResolvable(Type type)
        {
            return type.IsPublic &&
                !(type.IsPointer || type.IsByRef || type.IsGenericTypeDefinition) &&
                (!type.IsConstructedGenericType || type.GetGenericArguments().All(IsTypeResolvable));
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
            bool forAttribute = false)
        {
            var dropped = new HashSet<string>();
            var builder = new StringBuilder();
            GetTypeNameForLiteralImpl(type, dropNamespaces, dropped, builder);
            droppedNamespaces = dropped.ToArray();
            if (forAttribute)
            {
                return GetTypeNameForAttribute(builder.ToString());
            }

            return builder.ToString();
        }

        private static bool FilterIsImplementable(MemberInfo m, object criteria)
        {
            return m is MethodInfo method &&
                (method.Attributes.HasFlag(MethodAttributes.Public) ||
                method.Attributes.HasFlag(MethodAttributes.Family) ||
                method.Attributes.HasFlag(MethodAttributes.FamORAssem)) &&
                !method.IsGenericMethod &&
                IsTypeResolvable(method.ReturnType) &&
                method.GetParameters().All(p => IsTypeResolvable(p.ParameterType));
        }

        private static bool FilterIsNotImplementableRequired(MemberInfo m, object criteria)
        {
            return m is MethodInfo method &&
                method.IsAbstract &&
                !FilterIsImplementable(m, criteria);
        }

        private static void GetTypeNameForLiteralImpl(
            Type type,
            bool dropNamespaces,
            HashSet<string> droppedNamespaces,
            StringBuilder builder)
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

            if (dropNamespaces && !string.IsNullOrEmpty(type.Namespace))
            {
                droppedNamespaces.Add(type.Namespace);
            }
            else
            {
                builder.Append(type.Namespace).Append(Symbols.Dot);
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
                    builder);

                if (i < genericArgs.Length - 1)
                {
                    builder.Append(Symbols.Comma + Symbols.Space);
                }
            }

            builder.Append(Symbols.SquareClose);
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
