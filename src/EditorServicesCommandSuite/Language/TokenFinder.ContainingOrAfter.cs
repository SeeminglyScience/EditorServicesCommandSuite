using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal partial class TokenFinder
    {
        public static readonly TokenFilter<SimplePosition> ContainingOrAfterFilter
            = (in TokenNode node, SimplePosition position)
                => AfterFilter(in node, position) || ContainingFilter(in node, position);

        public TokenFinder ContainingOrAfter(IScriptPosition position) => ContainingOrAfter(new SimplePosition(position));

        public TokenFinder ContainingOrAfter(SimplePosition position)
        {
            return Where(ContainingOrAfterFilter, position);
        }

        public TokenFinder ContainingOrAfterStartOf(IScriptExtent extent) => ContainingOrAfter(extent.StartOffset);

        public TokenFinder ContainingOrAfterStartOf(SimpleRange range) => ContainingOrAfter(range.Start);

        public TokenFinder ContainingOrAfterEndOf(IScriptExtent extent) => ContainingOrAfter(extent.EndOffset);

        public TokenFinder ContainingOrAfterEndOf(SimpleRange range) => ContainingOrAfter(range.End);
    }
}
