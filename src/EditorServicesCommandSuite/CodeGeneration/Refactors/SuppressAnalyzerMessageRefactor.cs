using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    internal class SuppressAnalyzerMessageRefactor : AstRefactorProvider<Ast>
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

        internal override bool CanRefactorTarget(DocumentContextBase request, Ast ast)
        {
            if (string.IsNullOrWhiteSpace(ast.Extent.StartScriptPosition.GetFullScript()))
            {
                return false;
            }

            return GetMarkers(request).Any(
                marker => marker.Extent.ContainsOffset(request.SelectionExtent.EndOffset));
        }

        internal override async Task<IEnumerable<DocumentEdit>> RequestEdits(DocumentContextBase request, Ast ast)
        {
            var markers = (await GetMarkersAsync(request))
                .Where(marker => marker.Extent.ContainsOffset(request.SelectionExtent.EndOffset))
                .GroupBy(marker => marker.RuleName)
                .Select(markerGroup => markerGroup.First());

            return new SuppressHelper()
            {
                Markers = markers,
                Writer = new PowerShellScriptWriter(request.RootAst),
                Context = request,
            }
            .CreateEdits();
        }

        private IEnumerable<DiagnosticMarker> GetMarkers(DocumentContextBase request)
        {
            if (string.IsNullOrEmpty(request.RootAst.Extent.File) ||
                !File.Exists(request.RootAst.Extent.File))
            {
                return _analysisContext.GetDiagnosticsFromContents(
                    request.RootAst.Extent.Text,
                    request.CancellationToken);
            }

            return _analysisContext.GetDiagnosticsFromPath(
                request.RootAst.Extent.File,
                request.CancellationToken);
        }

        private async Task<IEnumerable<DiagnosticMarker>> GetMarkersAsync(DocumentContextBase request)
        {
            if (string.IsNullOrEmpty(request.RootAst.Extent.File) ||
                !File.Exists(request.RootAst.Extent.File))
            {
                return await _analysisContext.GetDiagnosticsFromContentsAsync(
                    request.RootAst.Extent.Text,
                    request.CancellationToken);
            }

            return await _analysisContext.GetDiagnosticsFromPathAsync(
                request.RootAst.Extent.File,
                request.CancellationToken);
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
                    }
                }

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

                var entryToken = Context.Token.At(sbAst);
                if (entryToken?.Value == null)
                {
                    Writer.SetPosition(sbAst);
                }
                else if (entryToken.Value.Kind == TokenKind.LCurly)
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
                if (entryToken == null || IsFollowedByBlankLine(entryToken))
                {
                    Writer.WriteLine();
                    return;
                }

                Writer.WriteLines(2);
            }

            private bool IsFollowedByBlankLine(LinkedListNode<Token> node)
            {
                return node
                    .EnumerateNext()
                    .TakeWhile(token => token.Value.Kind == TokenKind.NewLine)
                    .Count() > 1;
            }

            private bool IsFollowedByBlankLine(Ast ast)
            {
                return Context.Token.StartAtEndOf(ast)
                    .TakeWhile(token => token.Value.Kind == TokenKind.NewLine)
                    .Count() > 1;
            }

            private void WriteMarkers(IEnumerable<DiagnosticMarker> markers)
            {
                foreach (DiagnosticMarker marker in markers)
                {
                    Writer.OpenAttributeStatement(s_suppressTypeName);
                    Writer.WriteStringExpression(
                        StringConstantType.SingleQuoted,
                        marker.RuleName);
                    Writer.Write(Symbols.Comma + Symbols.Space);
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

                var tokenAtStatementStart = Context.Token.At(_parentStatementAst).Value;

                if (tokenAtStatementStart.TokenFlags.HasFlag(TokenFlags.StatementDoesntSupportAttributes))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
