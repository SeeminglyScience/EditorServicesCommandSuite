using System;
using System.Management.Automation.Language;

using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Language
{
    internal sealed partial class TokenFinder
    {
        public static readonly TokenFilter<SimplePosition> ContainingFilter;

        public static readonly TokenFilter<TokenKind> KindFilter;

        private readonly TokenFinderDirection _direction;

        private TokenNode _node;

        private TokenSearchStopper _stopper;

        private bool _searchFailed;

        private bool _includeSelf;

        private Exception _reason;

        static TokenFinder()
        {
            ContainingFilter = (in TokenNode node, SimplePosition offset)
                => node.Value?.Extent.ContainsOffset(offset) ?? false;

            KindFilter = (in TokenNode node, TokenKind kind) => node.Kind == kind;
        }

        public TokenFinder(TokenNode node, TokenFinderDirection direction, bool includeSelf = false)
        {
            _direction = direction;
            _includeSelf = includeSelf;
            _node = node;
            if (node.IsDefault)
            {
                _searchFailed = true;
                _reason = Error.AttemptedAccessDefaultTokenNode();
            }
        }

        public TokenNode GetResult()
        {
            return !_searchFailed
                ? _node
                : throw _reason ?? Error.TokenNotFound();
        }

        public TokenNode? GetResultOrNull()
        {
            return _searchFailed ? (TokenNode?)null : _node;
        }

        public bool TryGetResult(out TokenNode node)
        {
            if (_searchFailed)
            {
                node = default;
                return false;
            }

            node = _node;
            return true;
        }

        public bool TryGetResult(out TokenNode node, out Exception reason)
        {
            if (_searchFailed)
            {
                reason = _reason;
                node = default;
                return false;
            }

            node = _node;
            reason = null;
            return true;
        }

        public TokenFinder OnlyWithin(IScriptExtent extent)
            => OnlyWithin(new SimpleRange(extent));

        public TokenFinder OnlyWithin(SimpleRange range)
        {
            if (_searchFailed) return this;
            (int startOffset, int endOffset) = range;
            _stopper += (in TokenNode node, ref bool stopSearch) =>
            {
                if (stopSearch || node.IsDefault)
                {
                    stopSearch = true;
                    return;
                }

                if (_direction == TokenFinderDirection.Next)
                {
                    if (node.Value.Extent.EndOffset > endOffset)
                    {
                        stopSearch = true;
                    }

                    return;
                }

                if (_direction == TokenFinderDirection.Previous)
                {
                    if (node.Value.Extent.StartOffset < startOffset)
                    {
                        stopSearch = true;
                    }

                    return;
                }
            };

            return this;
        }

        public TokenFinder IncludeSelf()
        {
            _includeSelf = true;
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

        public TokenFinder ThenPrevious() => GetResult().FindPrevious();

        public TokenFinder ThenNext() => GetResult().FindNext();

        public TokenFinder Then() => _direction switch
        {
            TokenFinderDirection.Next => GetResult().FindNext(),
            TokenFinderDirection.Previous => GetResult().FindPrevious(),
            _ => throw new ArgumentOutOfRangeException(nameof(_direction)),
        };

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
            while (_includeSelf || TryMove(ref _node, _direction))
            {
                if (_includeSelf)
                {
                    _includeSelf = false;
                }

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
            while (_includeSelf || TryMove(ref _node, _direction))
            {
                if (_includeSelf)
                {
                    _includeSelf = false;
                }

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
