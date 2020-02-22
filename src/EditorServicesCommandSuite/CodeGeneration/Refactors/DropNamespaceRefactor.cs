using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Reflection;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsDiagnostic.Resolve, "TypeExpression")]
    internal class DropNamespaceRefactor : RefactorProvider
    {
        public override ImmutableArray<CodeAction> SupportedActions { get; }
            = ImmutableArray.Create(
                CodeAction.Inactive(CodeActionIds.AlterTypeExpression, "using namespace {0}"),
                CodeAction.Inactive(CodeActionIds.AlterTypeExpression, "Simplify name '{0}'"),
                CodeAction.Inactive(CodeActionIds.AlterTypeExpressionFullyQualify, "Fully qualify '{0}'", rank: -10));

        private CodeAction AddUsingAction => SupportedActions[0];

        private CodeAction SimplifyNameAction => SupportedActions[1];

        private CodeAction FullyQualifyAction => SupportedActions[2];

        public override async Task ComputeCodeActions(DocumentContextBase context)
        {
            ITypeName typeName = context.Ast is TypeConstraintAst constraintAst
                ? constraintAst.TypeName
                : (context.Ast as TypeExpressionAst)?.TypeName;

            if (typeName == null)
            {
                return;
            }

            typeName = FindTargetTypeName(typeName, context.SelectionExtent.StartScriptPosition);

            bool hasDot = typeName.Name.Contains(Symbols.Dot);
            Type resolvedType = typeName.GetReflectionType() ?? typeName.GetReflectionAttributeType();
            Type[] allResolvedTypes = MemberUtil.ResolveTypes(typeName.Name, searchFullName: hasDot);

            if (resolvedType != null && Array.IndexOf(allResolvedTypes, resolvedType) == -1)
            {
                var newTypes = new Type[allResolvedTypes.Length + 1];
                newTypes[0] = resolvedType;
                allResolvedTypes.CopyTo(newTypes, index: 1);
                allResolvedTypes = newTypes;
            }

            var existingNamespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usingStatements = context.RootAst.UsingStatements;
            if (usingStatements != null)
            {
                foreach (UsingStatementAst usingStatement in usingStatements)
                {
                    if (usingStatement.UsingStatementKind == UsingStatementKind.Namespace)
                    {
                        existingNamespaces.Add(usingStatement.Name.Value);
                    }
                }
            }

            foreach (Type type in allResolvedTypes)
            {
                if (string.IsNullOrEmpty(type.Namespace))
                {
                    continue;
                }

                if (hasDot)
                {
                    await context.RegisterCodeActionAsync(
                        CreateCodeAction(
                            SimplifyNameAction,
                            type.FullName,
                            type,
                            typeName))
                        .ConfigureAwait(false);
                    continue;
                }

                if (resolvedType == null && !existingNamespaces.Contains(type.Namespace))
                {
                    await context.RegisterCodeActionAsync(
                        CreateCodeAction(
                            AddUsingAction,
                            type.Namespace,
                            type,
                            typeName))
                        .ConfigureAwait(false);
                }

                await context.RegisterCodeActionAsync(
                    CreateCodeAction(
                        FullyQualifyAction,
                        type.FullName,
                        type,
                        typeName,
                        isExpand: true))
                    .ConfigureAwait(false);
            }
        }

        internal static async Task AlterTypeExpressionAsync(
            DocumentContextBase context,
            Type type,
            ITypeName typeName,
            bool isExpand)
        {
            var writer = new PowerShellScriptWriter(context.RootAst);
            writer.StartWriting(typeName.Extent);
            if (isExpand)
            {
                writer.Write(
                    MemberUtil.GetTypeNameForLiteral(
                        type,
                        dropNamespaces: false,
                        out _,
                        skipGenericArgs: true));

                writer.FinishWriting();
            }
            else
            {
                writer.WriteTypeExpression(
                    new PSTypeName(type),
                    writeBrackets: false,
                    shouldDropNamespaces: true,
                    skipGenericArgs: true);

                writer.FinishWriting();
            }

            await context
                .RegisterWorkspaceChangeAsync(writer.CreateWorkspaceChange(context))
                .ConfigureAwait(false);
        }

        private static ITypeName FindTargetTypeName(ITypeName typeName, IScriptPosition position)
        {
            while (true)
            {
                OuterLoop:
                if (typeName is GenericTypeName generic)
                {
                    if (generic.TypeName.Extent.ContainsPosition(position))
                    {
                        typeName = generic.TypeName;
                        continue;
                    }

                    foreach (ITypeName genericArg in generic.GenericArguments)
                    {
                        if (genericArg.Extent.ContainsPosition(position))
                        {
                            typeName = genericArg;
                            goto OuterLoop;
                        }
                    }

                    // Position isn't cleanly within any of them, fallback to the
                    // generic definition.
                    typeName = generic.TypeName;
                    continue;
                }

                if (typeName is ArrayTypeName array)
                {
                    typeName = array.ElementType;
                    continue;
                }

                return typeName;
            }
        }

        private static CodeAction CreateCodeAction(
            CodeAction source,
            string formatArg,
            Type type,
            ITypeName typeName,
            bool isExpand = false)
        {
            return source.With(
                factory: AlterTypeExpressionAsync,
                state: (type, typeName, isExpand),
                title: string.Format(
                    CultureInfo.CurrentCulture,
                    source.Title,
                    formatArg));
        }
    }
}
