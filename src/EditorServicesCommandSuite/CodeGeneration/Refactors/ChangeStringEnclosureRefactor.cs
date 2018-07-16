using System;
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
    [Refactor(VerbsData.Convert, "StringExpression")]
    [RefactorConfiguration(typeof(ChangeStringEnclosureConfiguration))]
    internal class ChangeStringEnclosureRefactor : TokenRefactorProvider<StringToken>
    {
        private static readonly StringEnclosureInfo[] s_enclosures = new StringEnclosureInfo[]
        {
            new StringEnclosureInfo(StringEnclosureType.Expandable, DoubleQuote, DoubleQuote),
            new StringEnclosureInfo(StringEnclosureType.ExpandableHereString, ExpandableHereStringOpen, ExpandableHereStringClose),
            new StringEnclosureInfo(StringEnclosureType.Literal, SingleQuote, SingleQuote),
            new StringEnclosureInfo(StringEnclosureType.LiteralHereString, LiteralHereStringOpen, LiteralHereStringClose),
        };

        private static readonly StringEnclosureInfo s_bareword =
            new StringEnclosureInfo(StringEnclosureType.BareWord, string.Empty, string.Empty);

        private static readonly char[] s_atSymbolSingleOrDoubleQuote = { SingleQuote, DoubleQuote, At };

        private readonly IRefactorUI _ui;

        internal ChangeStringEnclosureRefactor(IRefactorUI ui)
        {
            _ui = ui;
        }

        public override string Name => ChangeStringEnclosureStrings.ProviderDisplayName;

        public override string Description => ChangeStringEnclosureStrings.ProviderDisplayDescription;

        internal static Task<IEnumerable<DocumentEdit>> GetEdits(
            ScriptBlockAst rootAst,
            StringToken token,
            StringEnclosureType current,
            StringEnclosureType selected,
            ExpandableStringExpressionAst ast = null)
        {
            StringEnclosureInfo currentInfo = current == StringEnclosureType.BareWord
                ? s_bareword
                : s_enclosures.Single(info => info.Type == current);

            StringEnclosureInfo selectedInfo = selected == StringEnclosureType.BareWord
                ? s_bareword
                : s_enclosures.Single(info => info.Type == selected);

            return GetEdits(
                rootAst,
                token,
                currentInfo,
                selectedInfo);
        }

        internal override bool CanRefactorToken(DocumentContextBase request, StringToken token)
        {
            return !token.TokenFlags.HasFlag(TokenFlags.CommandName)
                || token.Text.IndexOfAny(s_atSymbolSingleOrDoubleQuote) == 0;
        }

        internal override async Task<IEnumerable<DocumentEdit>> RequestEdits(
            DocumentContextBase request,
            StringToken token)
        {
            var config = request.GetConfiguration<ChangeStringEnclosureConfiguration>();
            StringEnclosureInfo current = s_enclosures
                .Where(e => token.Extent.Text.StartsWith(e.Open))
                .DefaultIfEmpty(s_bareword)
                .First();

            StringEnclosureInfo selected;
            if (config.Type == StringEnclosureType.Prompt)
            {
                selected = await _ui.ShowChoicePromptAsync(
                    ChangeStringEnclosureStrings.EnclosureTypeMenuCaption,
                    ChangeStringEnclosureStrings.EnclosureTypeMenuMessage,
                    s_enclosures
                        .Concat(new StringEnclosureInfo[] { s_bareword })
                        .Where(e => e != current).ToArray(),
                    GetEnclosureDescription);
            }
            else
            {
                selected = s_enclosures.Single(e => e.Type == config.Type);
            }

            return await GetEdits(
                request.RootAst,
                token,
                current,
                selected,
                request.Ast.FindParent<ExpandableStringExpressionAst>());
        }

        private static Task<IEnumerable<DocumentEdit>> GetEdits(
            ScriptBlockAst rootAst,
            StringToken token,
            StringEnclosureInfo current,
            StringEnclosureInfo selected,
            ExpandableStringExpressionAst expandableAst = null)
        {
            var isConvertingToHereString =
                selected.Open.Contains(Symbols.At)
                && !current.Open.Contains(Symbols.At);

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
                        expandableAst?.NestedExpressions.ToArray() ?? Empty.Array<ExpressionAst>(),
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

        private string GetEnclosureDescription(StringEnclosureInfo enclosure)
        {
            if (enclosure.Type == StringEnclosureType.BareWord)
            {
                return ChangeStringEnclosureStrings.BareWordTypeDisplayName;
            }

            return new StringBuilder()
                .Append(enclosure.Open)
                .Append(Space)
                .Append(enclosure.Close)
                .Append(SpaceEnclosedDash)
                .Append(enclosure.Type)
                .ToString();
        }

        private class FormatExpressionConversionHelper
        {
            private IScriptExtent[] _nestedExtents;

            private bool _first = true;

            private int _formatIndex;

            private int _index;

            private int[] _expressionStarts;

            private int[] _expressionEnds;

            private bool[] _wasParenClose;

            private char[] _expression;

            private Dictionary<char, int> _duplicateIndexMap = new Dictionary<char, int>();

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
                    _nestedExtents = Empty.Array<IScriptExtent>();
                    _expressionStarts = Empty.Array<int>();
                    _expressionEnds = Empty.Array<int>();
                    _wasParenClose = Empty.Array<bool>();
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

        private class StringEnclosureInfo
        {
            internal StringEnclosureInfo(
                StringEnclosureType type,
                char open,
                char close)
                : this(type, open.ToString(), close.ToString())
            {
            }

            internal StringEnclosureInfo(
                StringEnclosureType type,
                char[] open,
                char[] close)
                : this(type, new string(open), new string(close))
            {
            }

            internal StringEnclosureInfo(
                StringEnclosureType type,
                string open,
                string close)
            {
                Type = type;
                Open = open;
                Close = close;
            }

            internal StringEnclosureType Type { get; set; }

            internal string Open { get; set; }

            internal string Close { get; set; }
        }
    }
}
