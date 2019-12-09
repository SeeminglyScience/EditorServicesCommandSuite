using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal partial class TokenFinder
    {
        public static readonly TokenFilter<SimplePosition> BeforeFilter
            = (in TokenNode node, SimplePosition position)
                => node.Value?.Extent.EndOffset < position.Offset;

        public TokenFinder Before(IScriptPosition position) => Before(new SimplePosition(position));

        public TokenFinder Before(SimplePosition position)
        {
            return Where(BeforeFilter, position);
        }

        public TokenFinder BeforeStartOf(IScriptExtent extent) => Before(extent.StartOffset);

        public TokenFinder BeforeStartOf(SimpleRange range) => Before(range.Start);

        public TokenFinder BeforeEndOf(IScriptExtent extent) => Before(extent.EndOffset);

        public TokenFinder BeforeEndOf(SimpleRange range) => Before(range.End);
    }
}
