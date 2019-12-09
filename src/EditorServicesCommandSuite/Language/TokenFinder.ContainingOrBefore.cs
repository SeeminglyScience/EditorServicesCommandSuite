using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal partial class TokenFinder
    {
        public static readonly TokenFilter<SimplePosition> ContainingOrBeforeFilter
            = (in TokenNode node, SimplePosition position)
                => BeforeFilter(in node, position) || ContainingFilter(in node, position);

        public TokenFinder ContainingOrBefore(IScriptPosition position)
            => ContainingOrBefore(new SimplePosition(position));

        public TokenFinder ContainingOrBefore(SimplePosition position)
        {
            return Where(ContainingOrBeforeFilter, position);
        }

        public TokenFinder ContainingOrBeforeStartOf(IScriptExtent extent) => ContainingOrBefore(extent.StartOffset);

        public TokenFinder ContainingOrBeforeStartOf(SimpleRange range) => ContainingOrBefore(range.Start);

        public TokenFinder ContainingOrBeforeEndOf(IScriptExtent extent) => ContainingOrBefore(extent.EndOffset);

        public TokenFinder ContainingOrBeforeEndOf(SimpleRange range) => ContainingOrBefore(range.End);
    }
}
