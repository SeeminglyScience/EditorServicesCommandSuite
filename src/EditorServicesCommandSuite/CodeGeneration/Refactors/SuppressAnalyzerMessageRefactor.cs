using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsCommon.Set, "RuleSuppression")]
    internal class SuppressAnalyzerMessageRefactor : RefactorProvider
    {
        private static readonly HashSet<string> s_paramBlockOnlyRules = new HashSet<string>()
        {
            "PSAvoidGlobalVars",
        };

        private static readonly PSTypeName s_suppressTypeName =
            new PSTypeName(typeof(SuppressMessageAttribute));

        private readonly IRefactorAnalysisContext _analysisContext;

        internal SuppressAnalyzerMessageRefactor(IRefactorAnalysisContext analysisContext)
        {
            Validate.IsNotNull(nameof(analysisContext), analysisContext);
            _analysisContext = analysisContext;
        }

        public override string Name { get; } = SuppressAnalyzerMessageStrings.ProviderDisplayName;

        public override string Description { get; } = SuppressAnalyzerMessageStrings.ProviderDisplayDescription;

        public override ImmutableArray<CodeAction> SupportedActions { get; } = ImmutableArray.Create(
            CodeAction.Inactive(CodeActionIds.SuppressDiagnostics, "Suppress warning '{0}'"));

        public override async Task ComputeCodeActions(DocumentContextBase context)
        {
            if (string.IsNullOrWhiteSpace(context.Ast.Extent.StartScriptPosition.GetFullScript()))
            {
                return;
            }

            IEnumerable<DiagnosticMarker> markers = await GetMarkersAsync(context).ConfigureAwait(false);
            foreach (DiagnosticMarker marker in markers)
            {
                if (marker.Extent.ContainsOffset(context.SelectionExtent.EndOffset))
                {
                    await context.RegisterCodeActionAsync(CreateAction(marker)).ConfigureAwait(false);
                }
            }
        }

        private static async Task SuppressMarker(DocumentContextBase context, DiagnosticMarker marker)
        {
            var helper = new SuppressHelper()
            {
                Context = context,
                Markers = new[] { marker },
                Writer = new PowerShellScriptWriter(context.RootAst),
            };

            helper.CreateEdits();
            await context.RegisterWorkspaceChangeAsync(
                helper.Writer.CreateWorkspaceChange(context))
                .ConfigureAwait(false);
        }

        private CodeAction CreateAction(DiagnosticMarker marker)
        {
            var sourceAction = SupportedActions[0];
            return sourceAction.With(
                SuppressMarker,
                marker,
                title: string.Format(
                    CultureInfo.CurrentCulture,
                    sourceAction.Title,
                    string.IsNullOrEmpty(marker.RuleSuppressionId)
                        ? marker.RuleName
                        : marker.RuleSuppressionId));
        }

        private async Task<IEnumerable<DiagnosticMarker>> GetMarkersAsync(DocumentContextBase request)
        {
            if (string.IsNullOrEmpty(request.RootAst.Extent.File) ||
                !File.Exists(request.RootAst.Extent.File))
            {
                return await _analysisContext.GetDiagnosticsFromContentsAsync(
                    request.RootAst.Extent.Text,
                    request.PipelineThread,
                    request.CancellationToken)
                    .ConfigureAwait(false);
            }

            return await _analysisContext.GetDiagnosticsFromPathAsync(
                request.RootAst.Extent.File,
                request.PipelineThread,
                request.CancellationToken)
                .ConfigureAwait(false);
        }

        private class SuppressHelper
        {
            private bool _statementAcceptsAttributes;

            private StatementAst _parentStatementAst;

            internal PowerShellScriptWriter Writer { get; set; }

            internal DocumentContextBase Context { get; set; }

            internal IEnumerable<DiagnosticMarker> Markers { get; set; }

            internal IEnumerable<DocumentEdit> CreateEdits()
            {
                _parentStatementAst = Context.Ast.FindParent<StatementAst>();
                _statementAcceptsAttributes = DoesStatementAcceptAttributes();

                if (!_statementAcceptsAttributes)
                {
                    WriteToParamBlock(Markers);
                    Writer.CreateDocumentEdits();
                    return Writer.Edits;
                }

                var markerGroups = Markers.GroupBy(
                    marker => s_paramBlockOnlyRules.Contains(marker.RuleName));

                foreach (var group in markerGroups)
                {
                    if (group.Key)
                    {
                        WriteToParamBlock(group);
                    }
                    else
                    {
                        WriteToStatement(group);
                        Writer.WriteIndentIfPending();
                    }
                }

                Writer.CreateDocumentEdits();
                return Writer.Edits;
            }

            private void WriteToStatement(IEnumerable<DiagnosticMarker> markers)
            {
                if (!markers.Any())
                {
                    return;
                }

                Writer.SetPosition(_parentStatementAst);
                WriteMarkers(markers);
            }

            private void WriteToParamBlock(IEnumerable<DiagnosticMarker> markers)
            {
                if (!markers.Any())
                {
                    return;
                }

                ScriptBlockAst sbAst;
                if (Context.Ast.TryFindParent<FunctionDefinitionAst>(
                    function => !(function.Parent == null || function.Parent is FunctionMemberAst),
                    maxDepth: int.MaxValue,
                    out FunctionDefinitionAst fdAst))
                {
                    sbAst = fdAst.Body;
                }
                else
                {
                    sbAst = Context.RootAst;
                }

                if (sbAst.ParamBlock != null)
                {
                    Writer.SetPosition(sbAst.ParamBlock);
                    WriteMarkers(markers);
                    Writer.WriteIndentIfPending();
                    return;
                }

                if (sbAst.FindAst<UsingStatementAst>() != null)
                {
                    var lastUsing = sbAst.FindAllAsts<UsingStatementAst>().Last();
                    Writer.SetPosition(lastUsing, atEnd: true);
                    Writer.WriteLines(2);
                    WriteMarkers(markers);
                    Writer.WriteParamBlock();
                    if (IsFollowedByBlankLine(lastUsing))
                    {
                        Writer.WriteLine();
                        return;
                    }

                    Writer.WriteLines(2);
                    return;
                }

                bool foundToken = Context.Token.List.First
                    .FindNext()
                    .ContainingStartOf(sbAst)
                    .TryGetResult(out TokenNode entryToken);

                if (!foundToken)
                {
                    Writer.SetPosition(sbAst);
                }
                else if (entryToken.Kind == TokenKind.LCurly)
                {
                    Writer.SetPosition(entryToken, atEnd: true);
                    Writer.FrameOpen();
                    WriteMarkers(markers);
                    Writer.WriteParamBlock();
                    if (sbAst.EndBlock != null && sbAst.EndBlock.Unnamed)
                    {
                        Writer.WriteLine();
                    }

                    return;
                }
                else
                {
                    Writer.SetPosition(entryToken);
                }

                WriteMarkers(markers);
                Writer.WriteParamBlock();
                if (!foundToken || IsFollowedByBlankLine(entryToken))
                {
                    Writer.WriteLine();
                    return;
                }

                Writer.WriteLines(2);
            }

            private bool IsFollowedByBlankLine(TokenNode node)
            {
                return node.TryGetNext(out node) && node.Kind == TokenKind.NewLine
                    && node.TryGetNext(out node) && node.Kind == TokenKind.NewLine;
            }

            private bool IsFollowedByBlankLine(Ast ast)
            {
                return Context.Token.FindNext().AfterEndOf(ast).TryGetResult(out TokenNode node)
                    && IsFollowedByBlankLine(node);
            }

            private void WriteMarkers(IEnumerable<DiagnosticMarker> markers)
            {
                foreach (DiagnosticMarker marker in markers)
                {
                    Writer.OpenAttributeStatement(s_suppressTypeName);
                    Writer.WriteStringExpression(
                        StringConstantType.SingleQuoted,
                        marker.RuleName);
                    Writer.Write(Symbols.Comma);
                    Writer.Write(Symbols.Space);
                    Writer.WriteStringExpression(
                        StringConstantType.SingleQuoted,
                        marker.RuleSuppressionId);
                    Writer.CloseAttributeStatement();
                    Writer.WriteLine();
                }
            }

            private bool DoesStatementAcceptAttributes()
            {
                if (_parentStatementAst is PipelineAst || _parentStatementAst is CommandAst)
                {
                    return false;
                }

                Token tokenAtStatementStart = Context.Token.List.First
                    .FindNextOrSelf()
                    .ContainingStartOf(_parentStatementAst)
                    .GetResult()
                    .Value;

                return (tokenAtStatementStart.TokenFlags & TokenFlags.StatementDoesntSupportAttributes) == 0;
            }
        }
    }
}
