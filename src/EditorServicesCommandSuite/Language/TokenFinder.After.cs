using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal partial class TokenFinder
    {
        public static readonly TokenFilter<SimplePosition> AfterFilter
            = (in TokenNode node, SimplePosition position)
                => node.Value?.Extent.StartOffset > position.Offset;

        public TokenFinder After(IScriptPosition position) => After(new SimplePosition(position));

        public TokenFinder After(SimplePosition position)
        {
            return Where(AfterFilter, position);
        }

        public TokenFinder AfterStartOf(IScriptExtent extent) => After(extent.StartOffset);

        public TokenFinder AfterStartOf(SimpleRange range) => After(range.Start);

        public TokenFinder AfterEndOf(IScriptExtent extent) => After(extent.EndOffset);

        public TokenFinder AfterEndOf(SimpleRange range) => After(range.End);
    }
}
