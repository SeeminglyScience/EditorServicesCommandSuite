using System.Diagnostics;
using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal partial class TokenFinder
    {
        private static readonly TokenFilter<State> ClosestToFilter
            = (in TokenNode node, State state) =>
            {
                IScriptExtent extent = node.Value.Extent;
                if (extent.ContainsOffset(state.Position.Offset))
                {
                    state.LastNode = node;
                    return true;
                }

                if (extent.EndOffset < state.Position.Offset)
                {
                    state.LastNode = node;
                }

                return false;
            };

        public TokenFinder ClosestTo(IScriptPosition position)
            => ClosestTo(new SimplePosition(position));

        public TokenFinder ClosestTo(SimplePosition position)
        {
            if (_searchFailed) return this;
            void Stopper(in TokenNode node, ref bool stopSearch)
            {
                if (stopSearch) return;
                stopSearch = node.Value.Extent.EndOffset >= position.Offset;
            }

            _stopper += Stopper;
            try
            {
                var state = new State(position);
                Where(ClosestToFilter, state, _ => null);
                _searchFailed = false;
                _reason = null;
                _node = state.LastNode;
                Debug.Assert(!state.LastNode.IsDefault, "ClosestTo should never return default.");
                return this;
            }
            finally
            {
                _stopper -= Stopper;
            }
        }

        public TokenFinder ClosestToStartOf(IScriptExtent extent) => ClosestTo(extent.StartOffset);

        public TokenFinder ClosestToStartOf(SimpleRange range) => ClosestTo(range.Start);

        public TokenFinder ClosestToEndOf(IScriptExtent extent) => ClosestTo(extent.EndOffset);

        public TokenFinder ClosestToEndOf(SimpleRange range) => ClosestTo(range.End);

        private sealed class State
        {
            public readonly SimplePosition Position;

            public TokenNode LastNode;

            public State(SimplePosition position) => Position = position;
        }
    }
}
