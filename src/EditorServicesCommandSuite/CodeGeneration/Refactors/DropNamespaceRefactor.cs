using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Reflection;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsDiagnostic.Resolve, "TypeExpression")]
    internal class DropNamespaceRefactor : AstRefactorProvider<Ast>
    {
        public override string Name { get; } = DropNamespaceStrings.ProviderDisplayName;

        public override string Description { get; } = DropNamespaceStrings.ProviderDisplayDescription;

        internal override bool CanRefactorTarget(DocumentContextBase request, Ast ast)
        {
            return (ast is TypeConstraintAst constraint
                    && TypeNameNeedsDropOrResolve(constraint.TypeName))
                || (ast is TypeExpressionAst expression
                    && TypeNameNeedsDropOrResolve(expression.TypeName));
        }

        internal override Task<IEnumerable<DocumentEdit>> RequestEdits(DocumentContextBase request, Ast ast)
        {
            var writer = new PowerShellScriptWriter(ast);
            var typeName = ast is TypeConstraintAst constraint
                ? new PSTypeName(constraint.TypeName)
                : ast is TypeExpressionAst expression
                    ? new PSTypeName(expression.TypeName)
                    : null;

            if (typeName.Type == null &&
                MemberUtil.TryGetResolvedType(typeName.Name, out Type resolvedType))
            {
                writer.AddUsingStatements(
                    new HashSet<string>(new[] { resolvedType.Namespace }),
                    out int replaceLength);

                writer.CreateDocumentEdits(replaceLength);
                return Task.FromResult(writer.Edits);
            }

            if (ast.Extent.Text.StartsWith(Symbols.SquareOpen))
            {
                writer.SetPosition(ast.Extent.StartScriptPosition.Offset + 1);
                writer.WriteTypeExpression(typeName, writeBrackets: false);
                writer.CreateDocumentEdits(ast.Extent.Text.Length - 2);
                return Task.FromResult(writer.Edits);
            }

            writer.SetPosition(ast.Extent.StartScriptPosition);
            writer.WriteTypeExpression(typeName, writeBrackets: false);
            writer.CreateDocumentEdits(ast.Extent.Text.Length);
            return Task.FromResult(writer.Edits);
        }

        private bool TypeNameNeedsDropOrResolve(ITypeName typeName)
        {
            return typeName.Name.Contains(Symbols.Dot)
                || (typeName.GetReflectionType() == null
                && typeName.GetReflectionAttributeType() == null);
        }
    }
}
