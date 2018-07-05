using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Utility;

using static EditorServicesCommandSuite.Internal.Symbols;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsCommon.Add, "SurroundingExpression")]
    [RefactorConfiguration(typeof(SurroundSelectedLinesSettings))]
    internal class SurroundSelectedLinesRefactor : SelectionRefactor
    {
        private static readonly SurroundOption[] s_options = InitializeOptions();

        private readonly IRefactorUI _ui;

        private readonly IRefactorNavigation _navigation;

        internal SurroundSelectedLinesRefactor(IRefactorUI ui, IRefactorNavigation navigation)
        {
            this._ui = ui;
            this._navigation = navigation;
        }

        public override string Name { get; } = SurroundSelectedLinesStrings.ProviderDisplayName;

        public override string Description { get; } = SurroundSelectedLinesStrings.ProviderDisplayDescription;

        internal static Task<IEnumerable<DocumentEdit>> GetEdits(
            ScriptBlockAst rootAst,
            ExpressionSurroundType type,
            ref IScriptExtent extent)
        {
            return GetEdits(
                rootAst,
                s_options.First(option => option.Type == type),
                ref extent);
        }

        internal override async Task<IEnumerable<DocumentEdit>> RequestEdits(DocumentContextBase request, IScriptExtent extent)
        {
            var config = request.GetConfiguration<SurroundSelectedLinesSettings>();
            SurroundOption optionChoice;
            if (config.SurroundType != null)
            {
                optionChoice = s_options.First(option => option.Type == config.SurroundType.Value);
            }
            else
            {
                // Displays: "<name> - <open> <close>"
                optionChoice = await _ui.ShowChoicePromptAsync(
                    SurroundSelectedLinesStrings.SurroundStatementTypeMenuCaption,
                    SurroundSelectedLinesStrings.SurroundStatementTypeMenuMessage,
                    s_options,
                    option =>
                        new StringBuilder()
                            .Append(option.Name)
                            .Append(Space, Dash, Space)
                            .Append(option.Open)
                            .Append(Space)
                            .Append(option.Close)
                            .ToString());
            }

            IEnumerable<DocumentEdit> edits = await GetEdits(
                request.RootAst,
                optionChoice,
                ref extent);

            if (optionChoice.RelativeCursorOffset != null)
            {
                request.SetCursorPosition(
                    extent.StartLineNumber,
                    extent.StartColumnNumber + optionChoice.RelativeCursorOffset.Value);
            }

            return edits;
        }

        private static SurroundOption[] InitializeOptions()
        {
            return new SurroundOption[]
            {
                SurroundOption.Create(
                    ExpressionSurroundType.IfStatement,
                    "if () {",
                    "}",
                    SurroundSelectedLinesStrings.IfStatement,
                    shouldEnterFrame: true,
                    relativeCursorOffset: 4),
                SurroundOption.Create(
                    ExpressionSurroundType.WhileStatement,
                    "while () {",
                    "}",
                    SurroundSelectedLinesStrings.WhileLoop,
                    shouldEnterFrame: true,
                    relativeCursorOffset: 7),
                SurroundOption.Create(
                    ExpressionSurroundType.ForeachStatement,
                    "foreach ($ in $all) {",
                    "}",
                    SurroundSelectedLinesStrings.ForEachStatement,
                    shouldEnterFrame: true,
                    relativeCursorOffset: 10),
                SurroundOption.Create(
                    ExpressionSurroundType.ParenExpression,
                    "(",
                    ")",
                    SurroundSelectedLinesStrings.ParenExpression,
                    shouldEnterFrame: false),
                SurroundOption.Create(
                    ExpressionSurroundType.ArrayInitializer,
                    "@(",
                    ")",
                    SurroundSelectedLinesStrings.ArrayInitializer,
                    shouldEnterFrame: false),
                SurroundOption.Create(
                    ExpressionSurroundType.DollarParenExpression,
                    @"$(",
                    ")",
                    SurroundSelectedLinesStrings.Subexpression,
                    shouldEnterFrame: false),
            };
        }

        private static Task<IEnumerable<DocumentEdit>> GetEdits(
            ScriptBlockAst rootAst,
            SurroundOption optionChoice,
            ref IScriptExtent extent)
        {
            var writer = new PowerShellScriptWriter(rootAst);
            if (!optionChoice.ShouldEnterFrame)
            {
                HandleInline(writer, ref extent, optionChoice.Open, optionChoice.Close);
            }
            else
            {
                HandleFrame(writer, ref extent, optionChoice.Open, optionChoice.Close);
            }

            return Task.FromResult(writer.Edits);
        }

        private static void HandleInline(
            PowerShellScriptWriter writer,
            ref IScriptExtent extent,
            string open,
            string close)
        {
            extent = PositionUtilities.ReduceBoundsWhitespace(extent);

            writer.SetPosition(extent);
            writer.Write(open);
            writer.SetPosition(extent, atEnd: true);
            writer.Write(close);
        }

        private static void HandleFrame(
            PowerShellScriptWriter writer,
            ref IScriptExtent extent,
            string open,
            string close)
        {
            // Get an extent representing the entire line including leading indents.
            // This is needed so we can properly normalize the indent when writing
            // the extent back.
            IScriptExtent selectedLines = PositionUtilities.GetFullLines(extent);

            // Get the extent except the leading indent on the first line. This serves
            // as our entry point for the edit writer so indent can be detected properly.
            extent = PositionUtilities
                .NewScriptExtent(
                    selectedLines,
                    PositionUtilities.GetLineTextStartOffset(selectedLines.StartScriptPosition),
                    selectedLines.EndOffset);

            writer.StartWriting(extent);
            writer.Write(open);
            writer.FrameOpen();
            writer.WriteLines(
                TextUtilities.NormalizeIndent(
                    TextUtilities.GetLines(selectedLines.Text),
                    writer.TabString));
            writer.FrameClose();
            writer.Write(close);
        }

        private class SurroundOption
        {
            private SurroundOption()
            {
            }

            internal string Open { get; set; }

            internal string Close { get; set; }

            internal string Name { get; set; }

            internal ExpressionSurroundType Type { get; set; }

            internal bool ShouldEnterFrame { get; set; }

            internal int? RelativeCursorOffset { get; set; }

            internal static SurroundOption Create(
                ExpressionSurroundType type,
                string open = null,
                string close = null,
                string name = null,
                bool shouldEnterFrame = true,
                int? relativeCursorOffset = null)
            {
                return new SurroundOption()
                {
                    Type = type,
                    Open = open,
                    Close = close,
                    Name = name,
                    ShouldEnterFrame = shouldEnterFrame,
                    RelativeCursorOffset = relativeCursorOffset,
                };
            }
        }
    }
}
