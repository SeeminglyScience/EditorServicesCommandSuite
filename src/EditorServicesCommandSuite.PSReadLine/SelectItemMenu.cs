using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal delegate string SelectItemRenderer<TItem>(int index, TItem item);

    internal delegate bool SelectItemMatcher<TItem>(int index, TItem item, string input);

    internal class SelectItemMenu<TItem>
    {
        // private const string ClearCurrentLineAnsiEscape = "\x1b[K";

        // private const string SetHorizontalCursorPositionAnsiEscape = "\x1b[{0}G";

        // private const string SetVerticalPositionAnsiEscape = "\x1b[{0}d";

        // private const string EnterAlternateBufferAnsiEscape = "\x1b[?1049h";

        // private const string ExitAlternateBufferAnsiEscape = "\x1b[?1049l";

        private static readonly MatchCollection s_emptyMatchCollection = Regex.Matches(string.Empty, "z");

        private readonly TextWriter _out;

        private readonly StringBuilder _input = new StringBuilder(Console.WindowWidth - 1, Console.WindowWidth - 1);

        private readonly Dictionary<TItem, string> _renderCache = new Dictionary<TItem, string>();

        private readonly int _maxLineLength;

        private readonly int _maxLines;

        private readonly RenderedItem[] _renderedItems;

        private int _renderedItemsLength;

        private Tuple<string, string> _wordBoundryPatternCache;

        private SelectItemMatcher<TItem> _matcher;

        private SelectItemRenderer<TItem> _renderer;

        private bool _isInputAccepted;

        private bool _isHeaderWritten;

        private int _inputLine;

        private int _cursorX;

        private int _lastItemsStartingLine;

        private int _lastItemsLineLength;

        private int _selectionIndex;

        private TItem _selectedItem;

        internal SelectItemMenu(string caption, string message, TItem[] items)
        {
            Caption = caption;
            Message = message;
            Items = items;
            _out = Console.Out;
            _maxLineLength = Console.BufferWidth - 1;
            _maxLines = Console.WindowHeight - 1;
            _renderedItems = new RenderedItem[Items.Length];
        }

        internal string Caption { get; }

        internal string Message { get; }

        internal TItem[] Items { get; }

        private SelectItemMatcher<TItem> ItemMatcher
        {
            get
            {
                if (_matcher != null)
                {
                    return _matcher;
                }

                return DefaultItemMatcher;
            }

            set
            {
                _matcher = value;
            }
        }

        private SelectItemRenderer<TItem> ItemRenderer
        {
            get
            {
                if (_renderer != null)
                {
                    return _renderer;
                }

                return DefaultItemRenderer;
            }

            set
            {
                _renderer = value;
            }
        }

        internal SelectItemMenu<TItem> RenderItem(SelectItemRenderer<TItem> renderer)
        {
            ItemRenderer = renderer;
            return this;
        }

        internal SelectItemMenu<TItem> IsMatch(SelectItemMatcher<TItem> matcher)
        {
            ItemMatcher = matcher;
            return this;
        }

        internal TItem Prompt()
        {
            return Bind();
        }

        internal TItem Bind()
        {
            using (Menus.NewAlternateBuffer())
            {
                return ReadChoice();
            }
        }

        private static string DefaultItemRenderer(int index, TItem item)
        {
            return LanguagePrimitives.ConvertTo<string>(item);
        }

        private bool DefaultItemMatcher(int index, TItem item, string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }

            return Regex.IsMatch(
                RenderItem(index, item),
                GetWordBoundryPattern(input),
                RegexOptions.IgnoreCase);
        }

        private string GetWordBoundryPattern(string input)
        {
            if (_wordBoundryPatternCache != null &&
                _wordBoundryPatternCache.Item1.Equals(input, StringComparison.Ordinal))
            {
                return _wordBoundryPatternCache.Item2;
            }

            string[] parts = Regex.Split(input, @"(?<=.)\s+(?=.)");
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = Regex.Escape(parts[i]);
            }

            _wordBoundryPatternCache = Tuple.Create(
                input,
                $"\\b({string.Join("|", parts)})");

            return _wordBoundryPatternCache.Item2;
        }

        private TItem ReadChoice()
        {
            Render();
            _isInputAccepted = false;
            while (!_isInputAccepted)
            {
                ConsoleKeyInfo key;
                var oldControlAsInput = Console.TreatControlCAsInput;
                Console.TreatControlCAsInput = true;
                try
                {
                    key = Console.ReadKey(intercept: true);
                }
                finally
                {
                    Console.TreatControlCAsInput = oldControlAsInput;
                }

                switch (key.Key)
                {
                    case ConsoleKey.Backspace:
                        DeleteBackwardWord(key, ConsoleModifiers.Control, DeleteBackward);
                        break;
                    case ConsoleKey.Escape: Clear(); break;
                    case ConsoleKey.Delete: DeleteForwardWord(key, ConsoleModifiers.Control, DeleteForward); break;
                    case ConsoleKey.Enter: Accept(); break;
                    case ConsoleKey.UpArrow: MoveSelectionUp(key); break;
                    case ConsoleKey.DownArrow: MoveSelectionDown(key); break;
                    case ConsoleKey.RightArrow: MoveRight(key); break;
                    case ConsoleKey.LeftArrow: MoveLeft(key); break;
                    case ConsoleKey.P: MoveSelectionUp(key, ConsoleModifiers.Alt); break;
                    case ConsoleKey.N: MoveSelectionDown(key, ConsoleModifiers.Alt); break;
                    case ConsoleKey.C: Break(key, ConsoleModifiers.Control); break;
                    case ConsoleKey.W: DeleteBackwardWord(key, ConsoleModifiers.Control); break;
                    case ConsoleKey.Home: StartOfLine(); break;
                    case ConsoleKey.End: EndOfLine(); break;
                    default: SelfInsert(key); break;
                }
            }

            return _selectedItem;
        }

        private void MoveLeft(ConsoleKeyInfo key)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                _cursorX = GetBackwardWordOffset();
                SetCursorX(_cursorX);
                return;
            }

            if (_cursorX == 0)
            {
                return;
            }

            _cursorX--;
            _out.Write(Ansi.Movement.CursorLeft);
        }

        private void MoveRight(ConsoleKeyInfo key)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                _cursorX = GetForwardWordOffset();
                SetCursorX(_cursorX);
                return;
            }

            if (_cursorX >= _input.Length - 1)
            {
                return;
            }

            _cursorX++;
            _out.Write(Ansi.Movement.CursorRight);
        }

        private void Break(
            ConsoleKeyInfo key,
            ConsoleModifiers requiredModifiers = default(ConsoleModifiers))
        {
            if (!key.Modifiers.HasFlag(requiredModifiers))
            {
                SelfInsert(key);
                return;
            }

            throw new OperationCanceledException();
        }

        private void StartOfLine()
        {
            _cursorX = 0;
            SetCursorX(0);
        }

        private void EndOfLine()
        {
            _cursorX = _input.Length - 1;
            SetCursorX(_cursorX);
        }

        private void Clear()
        {
            _input.Clear();
            _cursorX = 0;
            Render();
        }

        private void Accept()
        {
            _isInputAccepted = true;
        }

        private void DeleteBackward(ConsoleKeyInfo key)
        {
            if (_input.Length < 1 || _cursorX == 0)
            {
                return;
            }

            _input.Remove(_cursorX - 1, 1);
            _cursorX--;
            Render();
        }

        private void DeleteBackwardWord(
            ConsoleKeyInfo key,
            ConsoleModifiers requiredModifiers = default(ConsoleModifiers),
            Action<ConsoleKeyInfo> alternativeAction = null)
        {
            if (!key.Modifiers.HasFlag(requiredModifiers))
            {
                if (alternativeAction != null)
                {
                    alternativeAction(key);
                    return;
                }

                SelfInsert(key);
                return;
            }

            int deleteTo = GetBackwardWordOffset();
            _input.Remove(deleteTo, _cursorX - deleteTo);
            _cursorX = deleteTo;
            Render();
        }

        private void DeleteForwardWord(
            ConsoleKeyInfo key,
            ConsoleModifiers requiredModifiers = default(ConsoleModifiers),
            Action<ConsoleKeyInfo> alternativeAction = null)
        {
            if (!key.Modifiers.HasFlag(requiredModifiers))
            {
                if (alternativeAction != null)
                {
                    alternativeAction(key);
                    return;
                }

                SelfInsert(key);
                return;
            }

            int deleteTo = GetForwardWordOffset();
            _input.Remove(_cursorX, deleteTo - _cursorX);
            Render();
        }

        private int GetBackwardWordOffset()
        {
            if (_input.Length == 0)
            {
                return 0;
            }

            return Regex.Matches(_input.ToString(), @"\b")
                .Cast<Match>()
                .Where(match => match.Index < _cursorX)
                .OrderByDescending(match => match.Index)
                .First()
                .Index;
        }

        private int GetForwardWordOffset()
        {
            var offset = Regex.Matches(_input.ToString(), @"\b")
                .Cast<Match>()
                .Where(match => match.Index > _cursorX)
                .OrderBy(match => match.Index)
                .FirstOrDefault()
                ?.Index
                ?? -1;

            return offset == -1 ? _input.Length : offset;
        }

        private void DeleteForward(ConsoleKeyInfo key)
        {
            if (_input.Length < 1 || _cursorX == _input.Length)
            {
                return;
            }

            _input.Remove(_cursorX, 1);
            Render();
        }

        private void SelfInsert(ConsoleKeyInfo key)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Control) ||
                key.Modifiers.HasFlag(ConsoleModifiers.Alt))
            {
                return;
            }

            if (_input.MaxCapacity == _input.Length)
            {
                return;
            }

            _input.Insert(_cursorX, key.KeyChar);
            _cursorX++;
            Render();
        }

        private void MoveSelectionUp(
            ConsoleKeyInfo key, ConsoleModifiers requiredModifiers = default(ConsoleModifiers))
        {
            if (!key.Modifiers.HasFlag(requiredModifiers))
            {
                SelfInsert(key);
                return;
            }

            if (_selectionIndex == 0)
            {
                _selectionIndex = _renderedItemsLength - 1;
            }
            else
            {
                _selectionIndex--;
            }

            Render(isForSelectionChange: true);
        }

        private void MoveSelectionDown(
            ConsoleKeyInfo key,
            ConsoleModifiers requiredModifiers = default(ConsoleModifiers))
        {
            if (!key.Modifiers.HasFlag(requiredModifiers))
            {
                SelfInsert(key);
                return;
            }

            if (_selectionIndex >= _renderedItemsLength - 1)
            {
                _selectionIndex = 0;
            }
            else
            {
                _selectionIndex++;
            }

            Render(isForSelectionChange: true);
        }

        private void SetCursorX(int x)
        {
            _out.Write(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Ansi.Movement.SetHorizontalCursorPosition,
                    x + 1));
        }

        private void SetCursorY(int y)
        {
            _out.Write(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Ansi.Movement.SetVerticalCursorPosition,
                    y));
        }

        private void ClearLine()
        {
            _out.Write(Ansi.ClearCurrentLine);
        }

        private void Render(bool isForSelectionChange = false)
        {
            if (!_isHeaderWritten)
            {
                _out.Write(Ansi.Colors.Primary);
                _out.WriteLine(Caption);
                _out.Write(Ansi.Colors.Secondary);
                _out.WriteLine(Message);
                _out.WriteLine();
                _out.WriteLine();
                _inputLine = Console.CursorTop;
                _isHeaderWritten = true;
            }

            SetCursorY(_inputLine);
            SetCursorX(0);
            ClearLine();
            _out.WriteLine(_input);
            _out.WriteLine();
            _out.WriteLine();

            RenderItems(skipMatchRebuild: isForSelectionChange);
            SetCursorY(_inputLine);
            SetCursorX(_cursorX);
        }

        private int ReallyRenderItems(int currentMaxLines)
        {
            _selectionIndex = 0;
            string input = _input.ToString();
            int totalLines = 0;
            int matchIndex = 0;
            for (var i = 0; i < Items.Length; i++)
            {
                if (!ItemMatcher(i, Items[i], input))
                {
                    continue;
                }

                string renderedItem = RenderItem(i, Items[i]);
                int additionalLines =
                    renderedItem.Length <= _maxLineLength
                        ? 1
                        : (int)System.Math.Ceiling((double)renderedItem.Length / (double)_maxLineLength);
                if ((totalLines + additionalLines) >= currentMaxLines)
                {
                    break;
                }

                totalLines += additionalLines;
                MatchCollection matchCollection = string.IsNullOrWhiteSpace(input)
                    ? s_emptyMatchCollection
                    : Regex.Matches(
                        renderedItem,
                        GetWordBoundryPattern(input),
                        RegexOptions.IgnoreCase);

                _renderedItems[matchIndex] = new RenderedItem()
                {
                    Item = Items[i],
                    Index = i,
                    Matches = matchCollection,
                    Rank = CalculateRank(renderedItem, matchCollection),
                    Text = renderedItem,
                };

                matchIndex++;
            }

            _renderedItemsLength = matchIndex;
            Array.Sort(
                _renderedItems,
                index: 0,
                length: matchIndex,
                comparer: RenderedItemComparer.Instance);

            return totalLines;
        }

        private double CalculateRank(string renderedItem, MatchCollection matches)
        {
            int lastEndOffset = 0;
            int totalLength = 0;
            int consecutiveMatches = 0;
            var uniqueMatches = new HashSet<string>();
            for (var i = 0; i < matches.Count; i++)
            {
                if (i != 0)
                {
                    if (matches[i].Index == lastEndOffset - 1)
                    {
                        consecutiveMatches++;
                    }
                }

                lastEndOffset = matches[i].Index + matches[i].Length;
                uniqueMatches.Add(matches[i].Value);
                totalLength += matches[i].Length;
            }

            // Each unique matched string = 100 points
            // Matches that are close to each other = 50 points
            // Totals based on a percentage of total match coverage
            double multiplier = (double)totalLength / (double)renderedItem.Length;
            int tally = (uniqueMatches.Count * 100) + (consecutiveMatches * 50);
            return tally * (multiplier + 1D);
        }

        private void RenderItems(bool skipMatchRebuild = false)
        {
            int startingLine = Console.CursorTop;
            int currentMaxLines = _maxLines - startingLine;
            int totalLines = skipMatchRebuild ? _lastItemsLineLength : ReallyRenderItems(currentMaxLines);
            for (var i = 0; i < currentMaxLines; i++)
            {
                SetCursorY(i + startingLine);
                ClearLine();
            }

            SetCursorY(startingLine);
            _lastItemsStartingLine = startingLine;
            _lastItemsLineLength = totalLines;

            if (_renderedItemsLength == 0)
            {
                _selectedItem = default(TItem);
                return;
            }

            for (var i = 0; i < _renderedItemsLength; i++)
            {
                string colorEscape;
                bool isSelected = i == _selectionIndex;
                if (isSelected)
                {
                    _selectedItem = _renderedItems[i].Item;
                    colorEscape = Ansi.Colors.Selection;
                }
                else
                {
                    colorEscape = Ansi.Colors.Default;
                }

                char[] chars = _renderedItems[i].Text.ToCharArray();
                int lastIndex = 0;
                foreach (Match match in _renderedItems[i].Matches)
                {
                    Write(chars, lastIndex, match.Index - lastIndex);
                    WriteEmphasis(chars, match.Index, match.Length);
                    lastIndex = match.Index + match.Length;
                }

                Write(chars, lastIndex, chars.Length - lastIndex, newLine: true);
                // if (isSelected)
                // {
                //     _out.Write("\x1b[24m");
                // }
            }
        }

        private void Write(char[] buffer, int index, int count, bool newLine = false, string color = null)
        {
            if (string.IsNullOrEmpty(color))
            {
                _out.Write(Ansi.Colors.Default);
            }
            else
            {
                _out.Write(color);
            }

            _out.Write(buffer, index, count);

            if (newLine)
            {
                _out.WriteLine();
            }
        }

        private void WriteEmphasis(char[] buffer, int index, int count)
        {
            _out.Write(Ansi.Colors.Emphasis);
            _out.Write(buffer, index, count);
        }

        private string RenderItem(int index, TItem item)
        {
            string renderedItem;
            if (_renderCache.TryGetValue(item, out renderedItem))
            {
                return renderedItem;
            }

            renderedItem = string.Format(
                CultureInfo.InvariantCulture,
                "[{0}] {1}",
                index + 1,
                ItemRenderer(index, item));
            _renderCache.Add(item, renderedItem);
            return renderedItem;
        }

        private struct RenderedItem
        {
            internal TItem Item;

            internal int Index;

            internal double Rank;

            internal string Text;

            internal MatchCollection Matches;
        }

        private class RenderedItemComparer : IComparer<RenderedItem>
        {
            public static RenderedItemComparer Instance = new RenderedItemComparer();

            public int Compare(RenderedItem x, RenderedItem y)
            {
                if (x.Rank == y.Rank)
                {
                    return x.Index.CompareTo(y.Index);
                }

                return y.Rank.CompareTo(x.Rank);
            }
        }
    }
}
