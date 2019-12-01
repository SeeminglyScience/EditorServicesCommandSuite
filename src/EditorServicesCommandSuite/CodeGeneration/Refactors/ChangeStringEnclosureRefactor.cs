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

using static EditorServicesCommandSuite.Internal.Symbols;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsData.Convert, "StringExpression")]
    [RefactorConfiguration(typeof(ChangeStringEnclosureConfiguration))]
    internal partial class ChangeStringEnclosureRefactor : RefactorProvider
    {
        public override string Name => ChangeStringEnclosureStrings.ProviderDisplayName;

        public override string Description => ChangeStringEnclosureStrings.ProviderDisplayDescription;

        public override ImmutableArray<CodeAction> SupportedActions { get; } = ImmutableArray.Create<CodeAction>(
            new MyCodeAction(StringEnclosureInfo.Expandable),
            new MyCodeAction(StringEnclosureInfo.ExpandableHereString),
            new MyCodeAction(StringEnclosureInfo.Literal),
            new MyCodeAction(StringEnclosureInfo.LiteralHereString));

        private MyCodeAction ExpandableCodeAction => (MyCodeAction)SupportedActions[0];

        private MyCodeAction ExpandableHereStringCodeAction => (MyCodeAction)SupportedActions[1];

        private MyCodeAction LiteralCodeAction => (MyCodeAction)SupportedActions[2];

        private MyCodeAction LiteralHereStringCodeAction => (MyCodeAction)SupportedActions[3];

        public override async Task Invoke(DocumentContextBase context)
        {
            var config = context.GetConfiguration<ChangeStringEnclosureConfiguration>();
            if (!(context.Token.Value is StringToken token))
            {
                return;
            }

            if (config.Type == StringEnclosureType.Prompt)
            {
                await base.Invoke(context).ConfigureAwait(false);
                return;
            }

            StringEnclosureInfo current = DetectCurrentType(token) ?? StringEnclosureInfo.BareWord;
            MyCodeAction GetCodeAction(StringEnclosureType type)
            {
                return type switch
                {
                    StringEnclosureType.Expandable => ExpandableCodeAction,
                    StringEnclosureType.ExpandableHereString => ExpandableHereStringCodeAction,
                    StringEnclosureType.Literal => LiteralCodeAction,
                    StringEnclosureType.LiteralHereString => LiteralHereStringCodeAction,
                    _ => throw new ArgumentOutOfRangeException(nameof(type)),
                };
            }

            await ProcessActionForInvoke(
                context,
                GetCodeAction(config.Type).With(current, token))
                .ConfigureAwait(false);
        }

        public override async Task ComputeCodeActions(DocumentContextBase context)
        {
            if (!(context.Token.Value is StringToken token) || token.Value.Length < 2)
            {
                return;
            }

            StringEnclosureInfo current = DetectCurrentType(token);
            if (current == null)
            {
                return;
            }

            foreach (CodeAction action in SupportedActions)
            {
                var myAction = (MyCodeAction)action;
                if (current == myAction.Target)
                {
                    continue;
                }

                await context.RegisterCodeActionAsync(
                    myAction.With(current, token))
                    .ConfigureAwait(false);
            }
        }

        internal static async Task ProcessCodeActionAsync(
            DocumentContextBase context,
            StringEnclosureInfo target,
            StringEnclosureInfo current,
            StringToken token)
        {
            IEnumerable<DocumentEdit> edits = await GetEdits(
                context.RootAst,
                token,
                current,
                target,
                context.Ast.FindParent<ExpandableStringExpressionAst>(maxDepth: 3))
                .ConfigureAwait(false);

            await context.RegisterWorkspaceChangeAsync(
                WorkspaceChange.EditDocument(
                    context.Document,
                    edits))
                .ConfigureAwait(false);
        }

        private static Task<IEnumerable<DocumentEdit>> GetEdits(
            ScriptBlockAst rootAst,
            StringToken token,
            StringEnclosureInfo current,
            StringEnclosureInfo selected,
            ExpandableStringExpressionAst expandableAst = null)
        {
            var isConvertingToHereString =
                selected.Open.Contains(At)
                && !current.Open.Contains(At);

            var writer = new PowerShellScriptWriter(rootAst);

            writer.SetPosition(token.Extent);
            writer.Write(selected.Open);
            if (isConvertingToHereString)
            {
                writer.WriteLineNoIndent();
            }

            writer.CreateDocumentEdits(current.Open.Length);
            writer.SetPosition(token.Extent.EndOffset - current.Close.Length);
            if (isConvertingToHereString)
            {
                writer.WriteLineNoIndent();
            }

            writer.Write(selected.Close);
            writer.CreateDocumentEdits(current.Close.Length);

            if (current.Open.Contains(DoubleQuote) &&
                selected.Open.Contains(SingleQuote))
            {
                var helper = new FormatExpressionConversionHelper()
                {
                    IsSourceHereString = current.Type == StringEnclosureType.ExpandableHereString,
                    IsTargetHereString = selected.Type == StringEnclosureType.LiteralHereString,
                    FormatWriter = new PowerShellScriptWriter(rootAst),
                    Token = token,
                    NestedExpressions =
                        expandableAst?.NestedExpressions.ToArray() ?? Array.Empty<ExpressionAst>(),
                };

                helper.Convert();
                writer.SetPosition(token.Extent.StartOffset + current.Open.Length);
                helper.NewString.Remove(0, current.Open.Length);
                helper.NewString.Remove(
                    helper.NewString.Length - current.Close.Length,
                    current.Close.Length);
                writer.Write(helper.NewString.ToString());
                writer.CreateDocumentEdits(
                    token.Extent.Text.Length - current.Close.Length - current.Open.Length);
                return Task.FromResult(helper.FormatWriter.Edits.Concat(writer.Edits));
            }

            return Task.FromResult(writer.Edits);
        }

        private static StringEnclosureInfo DetectCurrentType(StringToken token)
        {
            string value = token?.Extent.Text;
            if (value == null || value.Length < 2)
            {
                return null;
            }

            return value[0] switch
            {
                SingleQuote => StringEnclosureInfo.Literal,
                DoubleQuote => StringEnclosureInfo.Expandable,
                At => value[1] switch
                {
                    SingleQuote => StringEnclosureInfo.LiteralHereString,
                    DoubleQuote => StringEnclosureInfo.ExpandableHereString,
                    _ => null,
#pragma warning disable SA1513
                },
#pragma warning restore SA1513
                _ => null,
            };
        }

        private class FormatExpressionConversionHelper
        {
            private readonly Dictionary<char, int> _duplicateIndexMap = new Dictionary<char, int>();

            private IScriptExtent[] _nestedExtents;

            private bool _first = true;

            private int _formatIndex;

            private int _index;

            private int[] _expressionStarts;

            private int[] _expressionEnds;

            private bool[] _wasParenClose;

            private char[] _expression;

            internal bool IsSourceHereString { get; set; }

            internal bool IsTargetHereString { get; set; }

            internal StringBuilder NewString { get; set; }

            internal PowerShellScriptWriter FormatWriter { get; set; }

            internal StringToken Token { get; set; }

            internal ExpressionAst[] NestedExpressions { get; set; }

            internal void Convert()
            {
                CreateExpressionReferences();
                for (; _index < _expression.Length; _index++)
                {
                    int startIndex = Array.IndexOf(_expressionStarts, _index);
                    if (startIndex != -1)
                    {
                        CreateFormat(startIndex);
                        continue;
                    }

                    switch (_expression[_index])
                    {
                        case Backtick:
                            // If a back tick is at the end of an expression and that
                            // expression was not a subexpression then it's probably
                            // terminating a variable name - which we can ignore.
                            int endIndex = Array.IndexOf(_expressionEnds, _index);
                            if (!(endIndex == -1 || _wasParenClose[endIndex]))
                            {
                                continue;
                            }

                            ProcessEscape();
                            break;
                        case CurlyOpen:
                            NewString.Append(CurlyOpen, 2);
                            break;
                        case CurlyClose:
                            NewString.Append(CurlyClose, 2);
                            break;
                        case SingleQuote:
                            if (!IsTargetHereString)
                            {
                                continue;
                            }

                            NewString.Append(SingleQuote, 2);
                            break;
                        default:
                            NewString.Append(_expression[_index]);
                            break;
                    }
                }
            }

            private void CreateExpressionReferences()
            {
                if (NestedExpressions.Length > 0)
                {
                    _nestedExtents = new IScriptExtent[NestedExpressions.Length];
                    for (var i = 0; i < NestedExpressions.Length; i++)
                    {
                        _nestedExtents[i] = NestedExpressions[i].Extent;
                    }

                    _expressionStarts = new int[_nestedExtents.Length];
                    _expressionEnds = new int[_nestedExtents.Length];
                    _wasParenClose = new bool[_nestedExtents.Length];
                }
                else
                {
                    _nestedExtents = Array.Empty<IScriptExtent>();
                    _expressionStarts = Array.Empty<int>();
                    _expressionEnds = Array.Empty<int>();
                    _wasParenClose = Array.Empty<bool>();
                }

                for (var i = 0; i < _nestedExtents.Length; i++)
                {
                    _expressionStarts[i] = _nestedExtents[i].StartOffset - Token.Extent.StartOffset;
                    _expressionEnds[i] = _nestedExtents[i].EndOffset - Token.Extent.StartOffset;
                    _wasParenClose[i] =
                        _nestedExtents[i].Text[_nestedExtents[i].Text.Length - 1] == ParenClose;
                }

                _expression = Token.Extent.Text.ToCharArray();
                NewString = new StringBuilder(_expression.Length);
            }

            private void ProcessEscape()
            {
                int formatIndex;
                char currentChar = _expression[_index + 1];
                switch (currentChar)
                {
                    case 'r':
                        if (_expression.Length > _index + 3 &&
                            _expression[_index + 2] == Backtick &&
                            _expression[_index + 3] == 'n' &&
                            Environment.NewLine == "\r\n")
                        {
                            if (_duplicateIndexMap.TryGetValue('l', out formatIndex))
                            {
                                CreateFormat(consumeLength: 4, formatIndex: formatIndex);
                                return;
                            }

                            _duplicateIndexMap.Add('l', _formatIndex);
                            CreateFormat(
                                consumeLength: 4,
                                writer: () => FormatWriter.Write(EnvironmentNewLine));

                            return;
                        }

                        if (_duplicateIndexMap.TryGetValue('r', out formatIndex))
                        {
                            CreateFormat(consumeLength: 2, formatIndex: formatIndex);
                            return;
                        }

                        _duplicateIndexMap.Add('r', _formatIndex);
                        CreateFormat(consumeLength: 2, shouldQuote: true);
                        return;
                    case 'n':
                        if (Environment.NewLine == "\n")
                        {
                            if (_duplicateIndexMap.TryGetValue('l', out formatIndex))
                            {
                                CreateFormat(consumeLength: 4, formatIndex: formatIndex);
                                return;
                            }

                            _duplicateIndexMap.Add('l', _formatIndex);
                            CreateFormat(
                                consumeLength: 4,
                                writer: () => FormatWriter.Write(EnvironmentNewLine));

                            return;
                        }

                        if (_duplicateIndexMap.TryGetValue('n', out formatIndex))
                        {
                            CreateFormat(consumeLength: 2, formatIndex: formatIndex);
                            return;
                        }

                        _duplicateIndexMap.Add('n', _formatIndex);
                        CreateFormat(consumeLength: 2, shouldQuote: true);
                        break;
                    case 'e':
                    case 't':
                    case 'v':
                    case 'b':
                    case 'f':
                    case '0':
                    case 'a':
                        if (_duplicateIndexMap.TryGetValue(currentChar, out formatIndex))
                        {
                            CreateFormat(consumeLength: 2, formatIndex: formatIndex);
                            return;
                        }

                        _duplicateIndexMap.Add(currentChar, _formatIndex);
                        CreateFormat(consumeLength: 2, shouldQuote: true);
                        break;
                    case 'u':
                        int endIndex = Array.IndexOf(_expression, CurlyClose, _index);

                        // If the unicode escape sequence isn't properly formed then just skip
                        // the backtick.
                        if (endIndex == -1)
                        {
                            break;
                        }

                        CreateFormat(endIndex - _index + 1, shouldQuote: true);
                        break;
                }
            }

            private void CreateFormat(int expressionStartIndex)
            {
                CreateFormat(
                    _expressionEnds[expressionStartIndex] - _expressionStarts[expressionStartIndex],
                    shouldQuote: false);
            }

            private void CreateFormat(
                int consumeLength,
                bool shouldQuote = false,
                Action writer = null,
                int? formatIndex = null)
            {
                if (formatIndex != null)
                {
                    NewString
                        .Append(CurlyOpen)
                        .Append(formatIndex.Value)
                        .Append(CurlyClose);

                    _index += consumeLength - 1;
                    return;
                }

                NewString
                    .Append(CurlyOpen)
                    .Append(_formatIndex)
                    .Append(CurlyClose);

                _formatIndex++;

                if (_first)
                {
                    FormatWriter.SetPosition(Token.Extent, atEnd: true);
                    FormatWriter.WriteChars(Symbols.Space, Symbols.Dash, 'f', Symbols.Space);
                    _first = false;
                }
                else
                {
                    FormatWriter.WriteChars(Symbols.Comma, Symbols.Space);
                }

                if (shouldQuote)
                {
                    FormatWriter.Write(DoubleQuote);
                }

                if (writer != null)
                {
                    writer();
                }
                else
                {
                    FormatWriter.Write(_expression, _index, consumeLength);
                }

                if (shouldQuote)
                {
                    FormatWriter.Write(DoubleQuote);
                }

                _index += consumeLength - 1;
            }
        }

        private class MyCodeAction : CodeAction
        {
            public MyCodeAction(
                StringEnclosureInfo target,
                StringEnclosureInfo current = null,
                StringToken token = null)
            {
                Target = target;
                Current = current;
                Token = token;
            }

            public override string Title => string.Format(
                CultureInfo.CurrentCulture,
                ChangeStringEnclosureStrings.RefactorStringTypeDescription,
                Target.Description);

            public override string Id => CodeActionIds.ChangeStringEnclosure;

            internal StringEnclosureInfo Target { get; }

            internal StringEnclosureInfo Current { get; }

            internal StringToken Token { get; }

            public override Task ComputeChanges(DocumentContextBase context)
            {
                return ProcessCodeActionAsync(
                    context,
                    Target,
                    Current,
                    Token);
            }

            internal MyCodeAction With(StringEnclosureInfo current, StringToken token)
            {
                return new MyCodeAction(Target, current, token);
            }
        }
    }
}
