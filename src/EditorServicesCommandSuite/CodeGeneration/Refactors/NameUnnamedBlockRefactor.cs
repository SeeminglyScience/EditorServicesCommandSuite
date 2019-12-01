using System.Collections.Generic;
using System.Collections.Immutable;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsData.ConvertFrom, "UnnamedBlock")]
    internal class NameUnnamedBlockRefactor : RefactorProvider
    {
        public override string Name { get; } = NameUnnamedBlockStrings.ProviderDisplayName;

        public override string Description { get; } = NameUnnamedBlockStrings.ProviderDisplayDescription;

        public override ImmutableArray<CodeAction> SupportedActions { get; } = ImmutableArray.Create(
            CodeAction.Inactive(CodeActionIds.NameUnnamedBlock, "Wrap in end { }", rank: -50));

        public override async Task ComputeCodeActions(DocumentContextBase context)
        {
            if (!context.Ast.TryFindParent(maxDepth: int.MaxValue, out NamedBlockAst namedBlock))
            {
                return;
            }

            if (!namedBlock.Unnamed)
            {
                return;
            }

            await context.RegisterCodeActionAsync(
                SupportedActions[0].With(ComputeUnnamedBlockNaming, namedBlock))
                .ConfigureAwait(false);
        }

        internal static async Task ComputeUnnamedBlockNaming(
            DocumentContextBase context,
            NamedBlockAst namedBlock)
        {
            IScriptExtent allStatements = namedBlock.Statements.JoinExtents();
            IScriptExtent fullLineStatements = PositionUtilities.GetFullLines(allStatements);
            var writer = new PowerShellScriptWriter(namedBlock);
            writer.StartWriting(allStatements);
            writer.Write(Symbols.End);
            writer.Write(Symbols.Space);
            writer.OpenScriptBlock();
            writer.WriteIndentNormalizedLines(fullLineStatements.Text);
            writer.CloseScriptBlock();
            writer.FinishWriting();
            await context.RegisterWorkspaceChangeAsync(writer.CreateWorkspaceChange(context)).ConfigureAwait(false);
        }
    }
}
