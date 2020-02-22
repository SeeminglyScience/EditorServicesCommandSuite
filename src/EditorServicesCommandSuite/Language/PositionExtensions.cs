using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Reflection;
using EditorServicesCommandSuite.Utility;

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
            if (position is Empty.Position)
            {
                Debug.Assert(
                    position.Offset == offset,
                    "Caller should verify CloneWithNewOffset is not called on EmptyPosition");
                return position;
            }

            Debug.Assert(
                position.GetType().Name == "InternalScriptPosition",
                "Caller should verify target of CloneWithNewOffset of a controlled type");

            return (IScriptPosition)ReflectionCache.InternalScriptPosition_CloneWithNewOffset
                .Invoke(position, new object[] { offset });
        }

        public static bool IsBefore(this IScriptExtent extentToTest, IScriptExtent startExtent)
        {
            return extentToTest.EndLineNumber < startExtent.StartLineNumber
                || (extentToTest.EndLineNumber == startExtent.StartLineNumber
                && extentToTest.EndColumnNumber <= startExtent.StartColumnNumber);
        }

        public static bool IsBefore(this IScriptPosition positionToTest, IScriptExtent startExtent)
        {
            return positionToTest.IsBefore(startExtent.StartScriptPosition);
        }

        public static bool IsBefore(this IScriptPosition positionToTest, IScriptPosition startPosition)
        {
            if (positionToTest.LineNumber < startPosition.LineNumber)
            {
                return true;
            }

            return positionToTest.LineNumber == startPosition.LineNumber
                && positionToTest.ColumnNumber < startPosition.ColumnNumber;
        }

        public static bool IsBeforeOrEqualTo(this IScriptPosition positionToTest, IScriptPosition other)
        {
            return positionToTest.IsEqualTo(other) || positionToTest.IsBefore(other);
        }

        public static bool IsAfter(this IScriptExtent extentToTest, IScriptExtent endExtent)
        {
            return extentToTest.StartLineNumber > endExtent.EndLineNumber
                || (extentToTest.StartLineNumber == endExtent.EndLineNumber
                && extentToTest.StartColumnNumber >= endExtent.EndColumnNumber);
        }

        public static bool IsAfter(this IScriptPosition positionToTest, IScriptExtent endExtent)
        {
            return positionToTest.IsAfter(endExtent.EndScriptPosition);
        }

        public static bool IsAfter(this IScriptPosition positionToTest, IScriptPosition endPosition)
        {
            if (positionToTest.LineNumber > endPosition.LineNumber)
            {
                return true;
            }

            return positionToTest.LineNumber == endPosition.LineNumber
                && positionToTest.ColumnNumber > endPosition.ColumnNumber;
        }

        public static bool IsAfterOrEqualTo(this IScriptPosition positionToTest, IScriptPosition other)
        {
            return positionToTest.IsAfter(other) || positionToTest.IsEqualTo(other);
        }

        public static bool IsWithin(this IScriptExtent extentToTest, IScriptExtent bounds)
        {
            var isOutsideOf = extentToTest.StartScriptPosition.IsBefore(bounds.StartScriptPosition)
                || extentToTest.EndScriptPosition.IsAfter(bounds.EndScriptPosition);

            return !isOutsideOf;
        }

        public static bool IsEqualTo(this IScriptPosition positionToTest, IScriptPosition other)
        {
            return positionToTest.ColumnNumber == other.ColumnNumber
                && positionToTest.LineNumber == other.LineNumber;
        }

        public static IScriptExtent JoinExtents(this IEnumerable<IScriptExtent> extents)
        {
            if (!extents.Any())
            {
                return Empty.Extent.Untitled;
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

        public static bool IsOffsetWithinOrDirectlyAfter(this IScriptExtent extent, int offsetToTest)
        {
            return ContainsOffset(extent, offsetToTest) || extent.EndOffset + 1 == offsetToTest;
        }

        public static bool ContainsPosition(this IScriptExtent extent, IScriptPosition position)
        {
            return ContainsOffset(extent, position.Offset);
        }

        public static bool ContainsOffset(this IScriptExtent extent, int offsetToTest)
        {
            return extent.StartOffset <= offsetToTest
                && extent.EndOffset >= offsetToTest;
        }

        public static bool ContainsExtent(this IScriptExtent extent, IScriptExtent extentToTest)
        {
            return extent.ContainsOffset(extentToTest.StartOffset)
                && extent.ContainsOffset(extentToTest.EndOffset);
        }

        public static bool HasRange(this IScriptExtent extent)
        {
            return extent.StartOffset != extent.EndOffset;
        }
    }
}
