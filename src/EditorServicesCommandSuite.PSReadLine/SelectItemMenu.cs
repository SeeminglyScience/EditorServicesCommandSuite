using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Text.RegularExpressions;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal delegate string SelectItemRenderer<TItem>(int index, TItem item);

    internal delegate bool SelectItemMatcher<TItem>(int index, TItem item, string input);

    internal class SelectItemMenu<TItem> : ConsoleBufferMenu<TItem>
    {
        private static readonly MatchCollection s_emptyMatchCollection = Regex.Matches(string.Empty, "z");

        private readonly Dictionary<TItem, string> _renderCache = new Dictionary<TItem, string>();

        private readonly Dictionary<TItem, string> _descriptionCache = new Dictionary<TItem, string>();

        private RenderedItem[] _renderedItems;

        private int _renderedItemsLength;

        private Tuple<string, string> _wordBoundryPatternCache;

        private SelectItemMatcher<TItem> _matcher;

        private SelectItemRenderer<TItem> _renderer;

        private int _resultsStartLine;

        private int _lastItemsStartingLine;

        private int _lastItemsLineLength;

        private int _lastRenderedLinesCount;

        private int _selectionIndex;

        private TItem _selectedItem;

        internal SelectItemMenu(string caption, string message, TItem[] items)
            : base(caption, message)
        {
            Items = items.Distinct().ToArray();
            _renderedItems = ArrayPool<RenderedItem>.Shared.Rent(Items.Length);
        }

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

        private SelectItemRenderer<TItem> ItemDescriptionRenderer { get; set; }

        internal SelectItemMenu<TItem> RenderItem(SelectItemRenderer<TItem> renderer)
        {
            ThrowIfDisposed();
            ItemRenderer = renderer;
            return this;
        }

        internal SelectItemMenu<TItem> RenderItemDescription(SelectItemRenderer<TItem> renderer)
        {
            ThrowIfDisposed();
            ItemDescriptionRenderer = renderer;
            return this;
        }

        internal SelectItemMenu<TItem> IsMatch(SelectItemMatcher<TItem> matcher)
        {
            ThrowIfDisposed();
            ItemMatcher = matcher;
            return this;
        }

        protected override void DisposeImpl()
        {
            ArrayPool<RenderedItem>.Shared.Return(_renderedItems);
            _renderedItems = null;
        }

        protected override TItem GetResult()
        {
            return _selectedItem;
        }

        protected override void AfterInputRender()
        {
            _out.Write(Ansi.Modification.InsertLines, 2);
            _out.Write(Ansi.Colors.Reset);
            _resultsStartLine = Console.CursorTop;
        }

        protected override void RenderBody(bool isForSelectionChange)
        {
            RenderItems(skipMatchRebuild: isForSelectionChange);
            SetCursorY(_inputLine);
            SetCursorX(_cursorX);
        }

        protected override void MoveSelectionDown(
            ConsoleKeyInfo key,
            ConsoleModifiers requiredModifiers = (ConsoleModifiers)0)
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

        protected override void MoveSelectionUp(
            ConsoleKeyInfo key,
            ConsoleModifiers requiredModifiers = (ConsoleModifiers)0)
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

        private int ReallyRenderItems(int currentMaxLines)
        {
            _selectionIndex = 0;
            string input = _input.ToString();
            int totalLines = 0;
            int matchIndex = 0;
            int maxLineLength = _lastWindowWidth - 1;
            for (var i = 0; i < Items.Length; i++)
            {
                if (!ItemMatcher(i, Items[i], input))
                {
                    continue;
                }

                string renderedItem = RenderItem(i, Items[i]);
                string description = RenderItemDescription(i, Items[i]);
                string fullString = $"{renderedItem} {description}";

                int additionalLines = Util.GetStringHeight(fullString, maxLineLength);
                if (additionalLines > 4)
                {
                    description = string.Empty;
                    additionalLines = Util.GetStringHeight(renderedItem, maxLineLength);
                    if (additionalLines > 4)
                    {
                        renderedItem = Regex
                            .Replace(renderedItem, @"(\r?\n)+", "; ")
                            .Substring(0, Math.Min(maxLineLength - 3, renderedItem.Length - 3))
                            + "...";

                        additionalLines = 1;
                    }
                }

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
                    Description = description,
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
            int currentMaxLines = _lastWindowHeight - _resultsStartLine;
            int totalLines = skipMatchRebuild ? _lastItemsLineLength : ReallyRenderItems(currentMaxLines);
            SetCursorX(0);
            for (var i = 0; i < _lastRenderedLinesCount; i++)
            {
                SetCursorY(i + _resultsStartLine);
                ClearLine();
            }

            _lastRenderedLinesCount = totalLines;

            SetCursorY(_resultsStartLine);
            _lastItemsStartingLine = _resultsStartLine;
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
                    Write(_renderedItems[i].Text, lastIndex, match.Index - lastIndex, color: colorEscape);
                    WriteEmphasis(_renderedItems[i].Text, match.Index, match.Length);
                    lastIndex = match.Index + match.Length;
                }

                Write(_renderedItems[i].Text, lastIndex, chars.Length - lastIndex, color: colorEscape);
                _out.Write(Ansi.Colors.Reset);
                if (!string.IsNullOrEmpty(_renderedItems[i].Description))
                {
                    _out.Write(Symbols.Space);
                    _out.Write(Ansi.Colors.Secondary);
                    _out.Write(_renderedItems[i].Description);
                    _out.Write(Ansi.Colors.Reset);
                }

                _out.WriteLine();
            }
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

        private string RenderItemDescription(int index, TItem item)
        {
            if (ItemDescriptionRenderer == null)
            {
                return string.Empty;
            }

            string renderedItem;
            if (_descriptionCache.TryGetValue(item, out renderedItem))
            {
                return renderedItem;
            }

            renderedItem = ItemDescriptionRenderer(index, item);
            _descriptionCache.Add(item, renderedItem);
            return renderedItem;
        }

        private struct RenderedItem
        {
            internal TItem Item;

            internal int Index;

            internal double Rank;

            internal string Text;

            internal string Description;

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
