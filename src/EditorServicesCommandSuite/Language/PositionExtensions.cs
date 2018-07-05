using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Reflection;

namespace EditorServicesCommandSuite.Language
{
    internal static class PositionExtensions
    {
        public static bool ContainsLineAndColumn(this IScriptExtent extent, int line, int column)
        {
            if (extent.StartLineNumber > line || extent.EndLineNumber < line)
            {
                return false;
            }

            if (extent.StartLineNumber == line && extent.EndLineNumber == line)
            {
                return extent.StartColumnNumber <= column && extent.EndColumnNumber >= column;
            }

            return (extent.StartLineNumber < line && extent.EndLineNumber > line)
                || (extent.StartLineNumber == line && extent.StartColumnNumber <= column)
                || (extent.EndLineNumber == line && extent.EndColumnNumber >= column);
        }

        public static IScriptPosition CloneWithNewOffset(this IScriptPosition position, int offset)
        {
            return (IScriptPosition)ReflectionCache.InternalScriptPosition_CloneWithNewOffset
                .Invoke(position, new object[] { offset });
        }

        public static bool IsBefore(this IScriptExtent extentToTest, IScriptExtent startExtent)
        {
            return extentToTest.EndLineNumber < startExtent.StartLineNumber
                || (extentToTest.EndLineNumber == startExtent.StartLineNumber
                && extentToTest.EndColumnNumber <= startExtent.StartColumnNumber);
        }

        public static bool IsAfter(this IScriptExtent extentToTest, IScriptExtent endExtent)
        {
            return extentToTest.StartLineNumber > endExtent.EndLineNumber
                || (extentToTest.StartLineNumber == endExtent.EndLineNumber
                && extentToTest.StartColumnNumber >= endExtent.EndColumnNumber);
        }

        public static IScriptExtent JoinExtents(this IEnumerable<IScriptExtent> extents)
        {
            if (!extents.Any())
            {
                return PositionUtilities.EmptyExtent;
            }

            return PositionUtilities.NewScriptExtent(
                extents.First(),
                extents.OrderBy(extent => extent.StartOffset).First().StartOffset,
                extents.OrderByDescending(extent => extent.EndOffset).First().EndOffset);
        }

        public static IScriptExtent JoinExtents(this IEnumerable<Ast> asts)
        {
            return JoinExtents(asts.Select(ast => ast.Extent));
        }

        public static IScriptExtent JoinExtents(this IEnumerable<Token> tokens)
        {
            return JoinExtents(tokens.Select(token => token.Extent));
        }

        public static bool ContainsOffset(this IScriptExtent extent, int offsetToTest)
        {
            return extent.StartOffset <= offsetToTest
                && extent.EndOffset >= offsetToTest;
        }
    }
}
