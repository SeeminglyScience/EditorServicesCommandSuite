using System;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Language
{
    internal partial class TokenFinder
    {
        public TokenFinder Where(TokenFilter filter)
        {
            return Where(filter, () => Error.TokenNotFound());
        }

        public TokenFinder Where<TState>(TokenFilter<TState> filter, TState state)
        {
            return Where(filter, state, state => Error.TokenNotFound(state));
        }

        private TokenFinder Where(TokenFilter filter, Func<Exception> exceptionFactory)
        {
            if (_searchFailed) return this;
            if (TryFind(filter, out TokenNode node))
            {
                _node = node;
                return this;
            }

            _searchFailed = true;
            _reason = exceptionFactory();
            return this;
        }

        private TokenFinder Where<TState>(
            TokenFilter<TState> filter,
            TState state,
            Func<TState, Exception> exceptionFactory)
        {
            if (_searchFailed) return this;
            if (TryFind(filter, state, out TokenNode node))
            {
                _node = node;
                return this;
            }

            _searchFailed = true;
            _reason = exceptionFactory(state);
            return this;
        }
    }
}
