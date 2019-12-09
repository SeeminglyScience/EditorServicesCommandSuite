using System.Management.Automation.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Language
{
    internal partial class TokenFinder
    {
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

        public TokenFinder ContainingStartOf(IScriptExtent extent) => Containing(extent.StartOffset);

        public TokenFinder ContainingStartOf(SimpleRange range) => Containing(range.Start);

        public TokenFinder ContainingEndOf(IScriptExtent extent) => Containing(extent.EndOffset);

        public TokenFinder ContainingEndOf(SimpleRange range) => Containing(range.End);
    }
}
