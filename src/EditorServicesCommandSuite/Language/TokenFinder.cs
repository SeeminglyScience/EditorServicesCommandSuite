using System;
using System.Management.Automation.Language;

using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Language
{
    internal sealed class TokenFinder
    {
        public static readonly TokenFilter<SimplePosition> ContainingFilter;

        public static readonly TokenFilter<TokenKind> KindFilter;

        private readonly TokenFinderDirection _direction;

        private TokenNode _node;

        private TokenSearchStopper _stopper;

        private bool _searchFailed;

        private Exception _reason;

        static TokenFinder()
        {
            ContainingFilter = (in TokenNode node, SimplePosition offset)
                => node.Value?.Extent.ContainsOffset(offset) ?? false;

            KindFilter = (in TokenNode node, TokenKind kind) => node.Kind == kind;
        }

        public TokenFinder(TokenNode node, TokenFinderDirection direction)
        {
            _node = node;
            _direction = direction;
        }

        public TokenNode GetResult()
        {
            return !_searchFailed
                ? _node
                : throw _reason ?? Error.TokenNotFound();
        }

        public TokenFinder OnlyWithin(IScriptExtent extent)
            => OnlyWithin(new SimpleRange(extent));

        public TokenFinder OnlyWithin(SimpleRange range)
        {
            if (_searchFailed) return this;
            (int startOffset, int endOffset) = range;

            Containing(startOffset);
            if (_searchFailed) return this;
            _stopper += (in TokenNode node, ref bool stopSearch) =>
            {
                var token = node.Value;
                if (token == null || stopSearch || token.Extent.StartOffset > endOffset)
                {
                    stopSearch = true;
                    return;
                }
            };

            return this;
        }

        public TokenFinder Containing(IScriptPosition position) => Containing(new SimplePosition(position));

        public TokenFinder Containing(SimplePosition position)
        {
            if (_searchFailed) return this;
            if (TryFind(ContainingFilter, position, out TokenNode node))
            {
                _node = node;
                return this;
            }

            _searchFailed = true;
            _reason = Error.TokenPositionNotFound(position);
            return this;
        }

        public TokenFinder OfKind(TokenKind kind)
        {
            if (_searchFailed) return this;
            if (TryFind(KindFilter, kind, out TokenNode node))
            {
                _node = node;
                return this;
            }

            _searchFailed = true;
            _reason = Error.TokenKindNotFound(kind);
            return this;
        }

        public TokenFinder Where(TokenFilter filter)
        {
            if (_searchFailed) return this;
            if (TryFind(filter, out TokenNode node))
            {
                _node = node;
                return this;
            }

            _searchFailed = true;
            _reason = Error.TokenNotFound();
            return this;
        }

        public TokenFinder Where<TState>(TokenFilter<TState> filter, TState state)
        {
            if (_searchFailed) return this;
            if (TryFind(filter, state, out TokenNode node))
            {
                _node = node;
                return this;
            }

            _searchFailed = true;
            _reason = Error.TokenNotFound(state);
            return this;
        }

        private static bool TryMove(ref TokenNode node, TokenFinderDirection direction)
        {
            return direction switch
            {
                TokenFinderDirection.Next => TokenNode.TryMoveNext(ref node),
                TokenFinderDirection.Previous => TokenNode.TryMovePrevious(ref node),
                _ => throw new ArgumentOutOfRangeException(nameof(direction)),
            };
        }

        private bool TryFind(TokenFilter filter, out TokenNode node)
        {
            bool stopSearch = false;
            while (TryMove(ref _node, _direction))
            {
                if (filter(in _node))
                {
                    node = _node;
                    return true;
                }

                _stopper?.Invoke(in _node, ref stopSearch);
                if (stopSearch)
                {
                    break;
                }
            }

            node = default;
            return false;
        }

        private bool TryFind<TState>(
            TokenFilter<TState> filter,
            TState state,
            out TokenNode node)
        {
            bool stopSearch = false;
            while (TryMove(ref _node, _direction))
            {
                if (filter(in _node, state))
                {
                    node = _node;
                    return true;
                }

                _stopper?.Invoke(in _node, ref stopSearch);
                if (stopSearch)
                {
                    break;
                }
            }

            node = default;
            return false;
        }
    }
}
