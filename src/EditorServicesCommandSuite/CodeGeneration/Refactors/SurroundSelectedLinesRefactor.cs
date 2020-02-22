using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    internal class SurroundSelectedLinesRefactor : RefactorProvider
    {
        private static readonly SurroundOption[] s_options = InitializeOptions();

        private readonly IRefactorUI _ui;

        internal SurroundSelectedLinesRefactor(IRefactorUI ui)
        {
            _ui = ui;
        }

        public override string Name { get; } = SurroundSelectedLinesStrings.ProviderDisplayName;

        public override string Description { get; } = SurroundSelectedLinesStrings.ProviderDisplayDescription;

        public override ImmutableArray<CodeAction> SupportedActions { get; } =
            ImmutableArray.Create(
                CodeAction.Inactive(CodeActionIds.SurroundSelectedLines, "Wrap selection in {0} - {1}", rank: 50));

        public override async Task ComputeCodeActions(DocumentContextBase context)
        {
            IScriptExtent selectionExtent = context.SelectionExtent;
            if (selectionExtent.StartOffset == selectionExtent.EndOffset)
            {
                return;
            }

            foreach (SurroundOption option in s_options)
            {
                await context.RegisterCodeActionAsync(CreateAction(option, selectionExtent))
                    .ConfigureAwait(false);
            }
        }

        public override async Task Invoke(DocumentContextBase context)
        {
            var config = context.GetConfiguration<SurroundSelectedLinesSettings>();
            if (config.SurroundType == ExpressionSurroundType.Prompt)
            {
                await base.Invoke(context).ConfigureAwait(false);
                return;
            }

            SurroundOption foundOption = Array.Find(s_options, o => o.Type == config.SurroundType);
            if (foundOption == null)
            {
                await _ui.ShowErrorMessageOrThrowAsync(
                    Error.OutOfRange,
                    nameof(config.SurroundType))
                    .ConfigureAwait(false);

                return;
            }

            await ProcessActionForInvoke(
                context,
                CreateAction(foundOption, context.SelectionExtent))
                .ConfigureAwait(false);
        }

        private static async Task SurroundSelectedLinesAsync(
            DocumentContextBase context,
            SurroundOption option,
            IScriptExtent selection)
        {
            var newSelection = selection;
            IEnumerable<DocumentEdit> edits = await GetEdits(
                context.RootAst,
                option,
                ref newSelection)
                .ConfigureAwait(false);

            await context.RegisterWorkspaceChangeAsync(
                WorkspaceChange.EditDocument(
                    context.Document,
                    edits))
                .ConfigureAwait(false);

            if (newSelection.StartOffset != selection.StartOffset || newSelection.EndOffset != selection.EndOffset)
            {
                await context.RegisterWorkspaceChangeAsync(
                    WorkspaceChange.SetContext(newSelection))
                    .ConfigureAwait(false);
            }
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
                    "$(",
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

            writer.WriteIndentNormalizedLines(selectedLines.Text);
            writer.FrameClose();
            writer.Write(close);
        }

        private CodeAction CreateAction(
            SurroundOption option,
            IScriptExtent selectionExtent)
        {
            var baseAction = SupportedActions[0];
            return baseAction.With(
                SurroundSelectedLinesAsync,
                state: (option, selectionExtent),
                title: string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    baseAction.Title,
                    option.Name,
                    string.Join(Space.ToString(), option.Open, option.Close)));
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
