using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsData.ConvertFrom, "UnnamedBlock")]
    internal class NameUnnamedBlockRefactor : AstRefactorProvider<NamedBlockAst>
    {
        public override string Name { get; } = NameUnnamedBlockStrings.ProviderDisplayName;

        public override string Description { get; } = NameUnnamedBlockStrings.ProviderDisplayDescription;

        public override int ParentSearchDepth => int.MaxValue;

        internal static Task<IEnumerable<DocumentEdit>> GetEdits(NamedBlockAst namedBlock)
        {
            Validate.IsNotNull(nameof(namedBlock), namedBlock);
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
            return Task.FromResult(writer.Edits);
        }

        internal override bool CanRefactorTarget(DocumentContextBase request, NamedBlockAst ast)
        {
            return ast.Unnamed;
        }

        internal override async Task<IEnumerable<DocumentEdit>> RequestEdits(DocumentContextBase request, NamedBlockAst ast)
        {
            return await GetEdits(ast);
        }
    }
}
