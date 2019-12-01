using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsData.ConvertTo, "FunctionDefinition")]
    [RefactorConfiguration(typeof(ExtractFunctionSettings))]
    internal class ExtractFunctionRefactor : RefactorProvider
    {
        private static readonly ExtractFunctionDestinationInfo[] s_destinationInfo =
        {
            new ExtractFunctionDestinationInfo(
                ExtractFunctionDestination.Begin,
                ExtractFunctionStrings.DestinationDisplayMessageBegin,
                ExtractFunctionStrings.DestinationHelpMessageBegin),
            new ExtractFunctionDestinationInfo(
                ExtractFunctionDestination.Inline,
                ExtractFunctionStrings.DestinationDisplayMessageInline,
                ExtractFunctionStrings.DestinationHelpMessageInline),
            new ExtractFunctionDestinationInfo(
                ExtractFunctionDestination.NewFile,
                ExtractFunctionStrings.DestinationDisplayMessageNewFile,
                ExtractFunctionStrings.DestinationHelpMessageNewFile),
        };

        private readonly IRefactorUI _ui;

        private readonly IRefactorWorkspace _workspace;

        internal ExtractFunctionRefactor(IRefactorUI ui, IRefactorWorkspace workspace)
        {
            _ui = ui;
            _workspace = workspace;
        }

        public override string Name { get; } = ExtractFunctionStrings.ProviderDisplayName;

        public override string Description { get; } = ExtractFunctionStrings.ProviderDisplayDescription;

        public override ImmutableArray<CodeAction> SupportedActions => ImmutableArray.Create(
            CodeAction.Inactive(CodeActionIds.ExtractFunction, "Extract function"),
            CodeAction.Inactive(CodeActionIds.ExtractFunction, "Extract function - to begin block"),
            CodeAction.Inactive(CodeActionIds.ExtractFunction, "Extract function - to {0}.ps1"));

        private string DefaultFunctionName => "Invoke-Function";

        private CodeAction DefaultCodeAction => SupportedActions[0];

        private CodeAction ToBeginBlockCodeAction => SupportedActions[1];

        private CodeAction ToFileCodeAction => SupportedActions[2];

        public override async Task ComputeCodeActions(DocumentContextBase context)
        {
            if (!context.SelectionExtent.HasRange())
            {
                return;
            }

            async Task Register(CodeAction source, ExtractFunctionDestination destination)
            {
                await context.RegisterCodeActionAsync(
                    source.With(
                        factory: ExtractFunctionAsync,
                        state: (destination, DefaultFunctionName, default(string))))
                        .ConfigureAwait(false);
            }

            await Register(DefaultCodeAction, ExtractFunctionDestination.Inline).ConfigureAwait(false);
            await Register(ToBeginBlockCodeAction, ExtractFunctionDestination.Begin).ConfigureAwait(false);
        }

        public override async Task Invoke(DocumentContextBase context)
        {
            ExtractFunctionSettings config = context.GetConfiguration<ExtractFunctionSettings>();
            ExtractFunctionDestination destination = config.Type;

            if (destination == ExtractFunctionDestination.Prompt)
            {
                var availableDestinations = s_destinationInfo;
                if (_workspace.IsUntitledWorkspace())
                {
                    availableDestinations = new ExtractFunctionDestinationInfo[]
                    {
                        s_destinationInfo[0],
                        s_destinationInfo[1],
                    };
                }

                destination = (await _ui.ShowChoicePromptAsync(
                    ExtractFunctionStrings.SelectDestinationCaption,
                    ExtractFunctionStrings.SelectDestinationMessage,
                    s_destinationInfo,
                    info => info.DisplayMessage,
                    info => info.HelpMessage)
                    .ConfigureAwait(false))
                    .Destination;
            }

            string functionName = config.FunctionName;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                functionName = await _ui.ShowInputPromptAsync(
                    ExtractFunctionStrings.FunctionNamePromptCaption,
                    ExtractFunctionStrings.FunctionNamePromptMessage)
                    .ConfigureAwait(false);
            }

            string filePath = config.FilePath;
            if (string.IsNullOrWhiteSpace(config.FilePath) &&
                destination == ExtractFunctionDestination.NewFile)
            {
                filePath = await _ui.ShowInputPromptAsync(
                    string.Concat(functionName, StringLiterals.ScriptFileExtension),
                    ExtractFunctionStrings.NewFilePathMessage)
                    .ConfigureAwait(false);

                ResolveFileName(ref filePath, functionName);
            }

            await ProcessActionForInvoke(
                context,
                DefaultCodeAction.With(
                    ExtractFunctionAsync,
                    (destination, functionName, filePath)))
                .ConfigureAwait(false);
        }

        internal static async Task<IEnumerable<DocumentEdit>> GetEdits(
            DocumentContextBase request,
            string functionName,
            ExtractFunctionDestination destination,
            ThreadController pipelineThread,
            string newFilePath = null,
            IRefactorUI ui = null)
        {
            return await GetEdits(
                new ExtractFunctionArguments()
                {
                    ClosestAst = request.Ast,
                    CurrentToken = request.Token,
                    Destination = destination,
                    PipelineThread = pipelineThread,
                    FunctionName = functionName,
                    RootAst = request.RootAst,
                    Selection = request.SelectionExtent,
                    UI = ui,
                    NewFilePath = newFilePath,
                }).ConfigureAwait(false);
        }

        private static async Task<IEnumerable<DocumentEdit>> GetEdits(ExtractFunctionArguments args)
        {
            args.Selection = PositionUtilities.GetFullLines(args.Selection);
            args.ParameterDetails =
                await SelectionVariableAnalysisVisitor.ProcessSelection(
                    args.RootAst,
                    args.Selection,
                    args.PipelineThread)
                    .ConfigureAwait(false);

            IScriptExtent restrictedSelection = PositionUtilities.NewScriptExtent(
                args.Selection,
                PositionUtilities.GetLineTextStartOffset(args.Selection.StartScriptPosition),
                PositionUtilities.GetLineEndOffset(args.Selection.EndScriptPosition));

            var writer = new PowerShellScriptWriter(args.RootAst);
            if (args.Destination == ExtractFunctionDestination.Inline)
            {
                ExtractToInline(args, writer);
            }
            else
            {
                writer.StartWriting(restrictedSelection);
            }

            writer.Write(args.FunctionName);
            if (args.ParameterDetails.Count != 0)
            {
                writer.Write(Symbols.Space);
            }

            writer.WriteEachWithSeparator(
                args.ParameterDetails.Values,
                parameter =>
                {
                    writer.Write(Symbols.Dash);
                    writer.WriteCasedString(parameter.VariableName.AsSpan(), CaseType.PascalCase);
                    writer.Write(Symbols.Space);
                    writer.WriteVariable(parameter.VariableName, CaseType.CamelCase);
                },
                Symbols.Space);

            writer.FinishWriting();
            if (args.Destination == ExtractFunctionDestination.Begin)
            {
                return await ExtractToBegin(args, writer).ConfigureAwait(false);
            }

            if (args.Destination == ExtractFunctionDestination.NewFile)
            {
                return await ExtractToNewFile(args, writer).ConfigureAwait(false);
            }

            return writer.Edits;
        }

        private static async Task<IEnumerable<DocumentEdit>> ExtractToBegin(
            ExtractFunctionArguments args,
            PowerShellScriptWriter writer)
        {
            // Find the smallest AST that contains the full selection, then find closest parent
            // named block that is not part of an expression.
            NamedBlockAst parentNamedBlock = args.RootAst
                .FindAstContaining(args.Selection)
                .FindParent<NamedBlockAst>(
                    ast => !((ast?.Parent as ScriptBlockAst)?.Parent is ScriptBlockExpressionAst),
                    maxDepth: int.MaxValue);

            if (parentNamedBlock.Unnamed)
            {
                await args.UI
                    .ShowErrorMessageOrThrowAsync(Error.CannotExtractFromUnnamed)
                    .ConfigureAwait(false);
            }

            WriteToBegin(
                args.CurrentToken,
                writer,
                () => WriteFunctionDefinition(writer, args),
                parentNamedBlock);

            return writer.Edits;
        }

        private static void WriteFunctionDefinition(
            PowerShellScriptWriter writer,
            ExtractFunctionArguments args)
        {
            writer.OpenFunctionDefinition(args.FunctionName);
            if (args.ParameterDetails.Count == 0)
            {
                writer.OpenNamedBlock(TokenKind.End);
                writer.WriteIndentNormalizedLines(args.Selection.Text);
                writer.CloseNamedBlock();
                writer.CloseFunctionDefinition();
                return;
            }

            var body = new StringBuilder(args.Selection.Text);
            writer.OpenParamBlock(shouldPushIndent: true);
            writer.WriteEachWithSeparator(
                args.ParameterDetails.Values,
                parameter =>
                {
                    writer.WriteTypeExpression(parameter.InferredType);
                    writer.Write(Symbols.Space);
                    writer.WriteVariable(parameter.VariableName, CaseType.PascalCase);
                },
                () =>
                {
                    writer.Write(Symbols.Comma);
                    writer.WriteLine();
                    writer.WriteLine();
                });

            writer.CloseParamBlock(shouldPopIndent: true);
            writer.WriteLine();
            writer.OpenNamedBlock(TokenKind.End);
            foreach (var details in args.ParameterDetails.Values)
            {
                foreach (VariableExpressionAst variable in details.Occurrences)
                {
                    int offset = variable.Extent.StartOffset - args.Selection.StartOffset;
                    body.Remove(offset, variable.Extent.Text.Length);

                    // Need to write the variable in reverse order since we are inserting at
                    // the same offset each time.
                    if (details.VariableName.Length > 1)
                    {
                        body.Insert(
                            offset,
                            details.VariableName.ToCharArray(),
                            1,
                            details.VariableName.Length - 1);
                    }

                    body.Insert(offset, char.ToUpperInvariant(details.VariableName[0]));
                    body.Insert(offset, Symbols.Dollar);
                }
            }

            writer.WriteIndentNormalizedLines(body.ToString());
            writer.CloseNamedBlock();
            writer.CloseFunctionDefinition();
        }

        private static void WriteToBegin(
            LinkedListNode<Token> closestToken,
            PowerShellScriptWriter writer,
            Action bodyWriter,
            NamedBlockAst parentBlock)
        {
            NamedBlockAst beginBlock = null;
            if (parentBlock.BlockKind == TokenKind.Begin)
            {
                beginBlock = parentBlock;
            }
            else if (((ScriptBlockAst)parentBlock.Parent).BeginBlock != null)
            {
                beginBlock = ((ScriptBlockAst)parentBlock.Parent).BeginBlock;
            }

            if (beginBlock != null)
            {
                writer.SetPosition(
                    closestToken
                        .AtOrBefore(beginBlock)
                        .FindNext(t => t.Value.Kind == TokenKind.LCurly),
                    atEnd: true);
                writer.PushIndent();
                writer.WriteLine();
                bodyWriter();
                if (beginBlock.Statements.Count != 0)
                {
                    writer.WriteLine();
                }

                writer.CreateDocumentEdits();
                return;
            }

            bool shouldWriteFinalLine = false;
            ScriptBlockAst bodyAst = (ScriptBlockAst)parentBlock.Parent;
            if (bodyAst.ParamBlock != null)
            {
                writer.SetPosition(bodyAst.ParamBlock, atEnd: true);
            }
            else
            {
                NamedBlockAst closestNamedBlock = bodyAst.ProcessBlock ?? bodyAst.EndBlock;
                writer.SetPosition(closestNamedBlock);
                shouldWriteFinalLine = true;
            }

            writer.Write(Symbols.Begin);
            writer.Write(Symbols.Space);
            writer.OpenScriptBlock();
            bodyWriter();
            writer.CloseScriptBlock();
            if (shouldWriteFinalLine)
            {
                writer.WriteLine();
                writer.WriteIndentIfPending();
            }

            writer.CreateDocumentEdits();
        }

        private static Task<IEnumerable<DocumentEdit>> ExtractToNewFile(
            ExtractFunctionArguments args,
            PowerShellScriptWriter mainWriter)
        {
            var writer = new PowerShellScriptWriter(args.NewFileAst, args.NewFilePath);
            writer.SetPosition(offset: 0);
            WriteFunctionDefinition(
                writer,
                args);

            if (!(args.NewFileAst is Empty.ScriptAst))
            {
                writer.WriteLines(amount: 2);
            }

            writer.CreateDocumentEdits();
            return Task.FromResult(mainWriter.Edits.Concat(writer.Edits));
        }

        private static void ExtractToInline(
            ExtractFunctionArguments args,
            PowerShellScriptWriter writer)
        {
            IScriptExtent restrictedSelection = PositionUtilities.NewScriptExtent(
                args.Selection,
                PositionUtilities.GetLineTextStartOffset(args.Selection.StartScriptPosition),
                PositionUtilities.GetLineEndOffset(args.Selection.EndScriptPosition));

            writer.StartWriting(restrictedSelection);
            WriteFunctionDefinition(writer, args);
            writer.WriteLines(2);
        }

        private void ResolveFileName(ref string filePath, string functionName)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = string.Concat(functionName, StringLiterals.ScriptFileExtension);
            }

            string resolvedFunctionPath = null;
            foreach (string path in Settings.FunctionPaths)
            {
                _workspace.TryResolveRelativePath(
                    path,
                    out bool doesExist,
                    out resolvedFunctionPath);

                if (doesExist)
                {
                    break;
                }
            }

            if (resolvedFunctionPath != null)
            {
                filePath = System.IO.Path.Combine(
                    resolvedFunctionPath,
                    filePath);
            }
            else
            {
                _workspace.TryResolveRelativePath(
                    filePath,
                    out _,
                    out filePath);
            }
        }

        private async Task ExtractFunctionAsync(
            DocumentContextBase context,
            ExtractFunctionDestination destination,
            string functionName,
            string filePath)
        {
            var args = new ExtractFunctionArguments()
            {
                    ClosestAst = context.Ast,
                    CurrentToken = context.Token,
                    Destination = destination,
                    PipelineThread = context.PipelineThread,
                    FunctionName = functionName,
                    RootAst = context.RootAst,
                    Selection = context.SelectionExtent,
                    UI = _ui,
                    NewFilePath = filePath,
            };

            IEnumerable<DocumentEdit> edits = await GetEdits(args).ConfigureAwait(false);
            foreach (WorkspaceChange change in WorkspaceChange.EditDocuments(edits))
            {
                await context.RegisterWorkspaceChangeAsync(change).ConfigureAwait(false);
            }
        }

        private struct ExtractFunctionArguments
        {
            internal string FunctionName;

            internal IScriptExtent Selection;

            internal ScriptBlockAst RootAst;

            internal Ast ClosestAst;

            internal LinkedListNode<Token> CurrentToken;

            internal ExtractFunctionDestination Destination;

            internal IRefactorUI UI;

            internal ThreadController PipelineThread;

            internal Dictionary<string, SelectionVariableAnalysisResult> ParameterDetails;

            internal string NewFilePath;

            internal ScriptBlockAst NewFileAst;
        }

        private readonly struct ExtractFunctionDestinationInfo
        {
            internal readonly ExtractFunctionDestination Destination;

            internal readonly string DisplayMessage;

            internal readonly string HelpMessage;

            internal ExtractFunctionDestinationInfo(
                ExtractFunctionDestination destination,
                string displayMessage,
                string helpMessage)
            {
                Destination = destination;
                DisplayMessage = displayMessage;
                HelpMessage = helpMessage;
            }
        }

        private readonly struct VariableReplacement
        {
            internal readonly string NewName;

            internal readonly VariableExpressionAst VariableExpression;

            internal VariableReplacement(string newName, VariableExpressionAst variableExpressionAst)
            {
                NewName = newName;
                VariableExpression = variableExpressionAst;
            }
        }
    }
}
