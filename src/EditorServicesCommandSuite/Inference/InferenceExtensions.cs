using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Reflection;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Inference
{
    internal static class InferenceExtensions
    {
        public static bool EqualsOrdinalIgnoreCase(this string s, string t)
        {
            return string.Equals(s, t, StringComparison.OrdinalIgnoreCase);
        }

        internal static async Task<IEnumerable<MemberInfo>> GetInferredMembers(
            this MemberExpressionAst ast,
            ThreadController threadController,
            CancellationToken cancellationToken = default,
            bool skipArgumentCheck = false,
            bool includeNonPublic = false)
        {
            IList<PSTypeName> expressionTypes;
            if (ast.Static && ast.Expression is TypeExpressionAst typeExpression)
            {
                PSTypeName resolvedType = ResolvePartialTypeName(new PSTypeName(typeExpression.TypeName));
                if (resolvedType == null)
                {
                    return Enumerable.Empty<MemberInfo>();
                }

                if (!resolvedType.Type.IsPublic && !includeNonPublic)
                {
                    return Enumerable.Empty<MemberInfo>();
                }

                expressionTypes = new[] { resolvedType };
            }
            else
            {
                expressionTypes = await AstTypeInference.InferTypeOf(
                    ast.Expression,
                    threadController,
                    cancellationToken,
                    includeNonPublic);
            }

            StringConstantExpressionAst memberNameConstant = ast.Member as StringConstantExpressionAst;
            if (memberNameConstant == null)
            {
                return Enumerable.Empty<MemberInfo>();
            }

            string memberName = memberNameConstant.Value.EqualsOrdinalIgnoreCase("new")
                ? ".ctor"
                : memberNameConstant.Value;
            InvokeMemberExpressionAst invokeExpression = ast as InvokeMemberExpressionAst;
            int argumentCount = invokeExpression?.Arguments.Count ?? 0;
            bool isStatic = ast.Static && !memberName.EqualsOrdinalIgnoreCase(".ctor");

            return EnumerateMembers();

            IEnumerable<MemberInfo> EnumerateMembers()
            {
                foreach (MemberInfo member in GetMembersForInferredTypes(expressionTypes, isStatic, includeNonPublic))
                {
                    if (member.Name.EqualsOrdinalIgnoreCase(memberName) &&
                        (invokeExpression == null || !(member is MethodBase)) &&
                        (skipArgumentCheck || argumentCount == member.GetParameterCount()))
                    {
                        yield return member;
                    }
                }
            }
        }

        private static int GetParameterCount(this MemberInfo member)
        {
            if (member is MethodBase method)
            {
                return method.GetParameters().Length;
            }

            return 0;
        }

        private static IEnumerable<MemberInfo> GetMembersForInferredTypes(
            IList<PSTypeName> inferredTypes,
            bool isStatic = false,
            bool includeNonPublic = false)
        {
            for (var i = 0; i < inferredTypes.Count; i++)
            {
                if (inferredTypes[i].Type == null)
                {
                    continue;
                }

                foreach (var member in MemberUtil.GetMembers(inferredTypes[i].Type, isStatic, includeNonPublic))
                {
                    yield return member;
                }
            }
        }

        private static PSTypeName ResolvePartialTypeName(PSTypeName typeName)
        {
            if (typeName.Type != null)
            {
                return typeName;
            }

            Type resolvedType;
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in loadedAssemblies)
            {
                resolvedType = assembly.GetType(typeName.Name, throwOnError: false, ignoreCase: true);
                if (resolvedType != null)
                {
                    return new PSTypeName(resolvedType);
                }
            }

            resolvedType = loadedAssemblies
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t => t.Name.EqualsOrdinalIgnoreCase(typeName.Name));

            return resolvedType == null ? null : new PSTypeName(resolvedType);
        }
    }
}
