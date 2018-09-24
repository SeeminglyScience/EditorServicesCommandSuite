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
        internal static PSCommand AddCommandWithPreferenceSetting(
            this PSCommand psCommand,
            string command)
        {
            return psCommand
                .AddCommand(command)
                .AddParameter("ErrorAction", ActionPreference.Ignore)
                .AddParameter("WarningAction", ActionPreference.Ignore)
                .AddParameter("InformationAction", ActionPreference.Ignore)
                .AddParameter("Verbose", false)
                .AddParameter("Debug", false);
        }

        internal static async Task<PSTypeName> GetInferredTypeAsync(
            this Ast ast,
            ThreadController pipelineThread,
            CancellationToken cancellationToken = default,
            bool includeNonPublic = false,
            PSTypeName defaultValue = null)
        {
            if (defaultValue != null)
            {
                return (await ast.GetInferredTypesAsync(pipelineThread, cancellationToken, includeNonPublic))
                    .DefaultIfEmpty(defaultValue)
                    .FirstOrDefault();
            }

            return (await ast.GetInferredTypesAsync(pipelineThread, cancellationToken, includeNonPublic))
                .FirstOrDefault();
        }

        internal static async Task<IList<PSTypeName>> GetInferredTypesAsync(
            this Ast ast,
            ThreadController pipelineThread,
            CancellationToken cancellationToken = default,
            bool includeNonPublic = false)
        {
            return await pipelineThread.InvokeAsync(
                () => Inference.AstTypeInference.InferTypeOf(
                    ast,
                    pipelineThread,
                    includeNonPublic,
                    cancellationToken),
                cancellationToken);
        }

        internal static async Task<IEnumerable<MemberInfo>> GetInferredMembersAsync(
            this MemberExpressionAst ast,
            ThreadController pipelineThread,
            bool skipArgumentCheck = false,
            bool includeNonPublic = false,
            CancellationToken cancellationToken = default)
        {
            IList<PSTypeName> expressionTypes;
            if (ast.Static && ast.Expression is TypeExpressionAst typeExpression)
            {
                PSTypeName resolvedType = ResolvePartialTypeName(new PSTypeName(typeExpression.TypeName));
                if (resolvedType == null)
                {
                    return Array.Empty<MemberInfo>();
                }

                if (!resolvedType.Type.IsPublic && !includeNonPublic)
                {
                    return Array.Empty<MemberInfo>();
                }

                expressionTypes = new[] { resolvedType };
            }
            else
            {
                expressionTypes = await pipelineThread.InvokeAsync(
                    () => AstTypeInference.InferTypeOf(
                        ast.Expression,
                        pipelineThread,
                        TypeInferenceRuntimePermissions.AllowSafeEval,
                        includeNonPublic,
                        cancellationToken),
                    cancellationToken);
            }

            StringConstantExpressionAst memberNameConstant = ast.Member as StringConstantExpressionAst;
            if (memberNameConstant == null)
            {
                return Array.Empty<MemberInfo>();
            }

            string memberName = memberNameConstant.Value.EqualsOrdinalIgnoreCase("new")
                ? ".ctor"
                : memberNameConstant.Value;
            InvokeMemberExpressionAst invokeExpression = ast as InvokeMemberExpressionAst;
            int argumentCount = invokeExpression?.Arguments.Count ?? 0;
            bool isStatic = ast.Static && !memberName.EqualsOrdinalIgnoreCase(".ctor");

            return EnumerateMembersForInferredTypes();

            IEnumerable<MemberInfo> EnumerateMembersForInferredTypes()
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

        private static int GetParameterCount(this MemberInfo member)
        {
            if (member is MethodBase method)
            {
                return method.GetParameters().Length;
            }

            return 0;
        }
    }
}
