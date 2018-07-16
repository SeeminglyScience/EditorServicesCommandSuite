using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Reflection;

namespace EditorServicesCommandSuite.Language
{
    internal static class PositionUtilities
    {
        internal static IScriptExtent NewScriptExtent(
            IScriptExtent source,
            int newStartOffset,
            int newEndOffset)
        {
            var positionHelper = ReflectionCache.InternalScriptExtent_PositionHelper
                .GetValue(source);

            return (IScriptExtent)ReflectionCache.InternalScriptExtent_Ctor
                .Invoke(new[] { positionHelper, newStartOffset, newEndOffset });
        }

        internal static int[] GetLineMap(string text)
        {
            List<int> lineStartMap = new List<int>(100) { 0 };
            for (int i = 0; i < text.Length; ++i)
            {
                char c = text[i];

                if (c == '\r')
                {
                    if ((i + 1) < text.Length && text[i + 1] == '\n')
                    {
                        i += 1;
                    }

                    lineStartMap.Add(i + 1);
                }

                if (c == '\n')
                {
                    lineStartMap.Add(i + 1);
                }
            }

            return lineStartMap.ToArray();
        }

        internal static int GetOffsetFromPosition(string text, int line, int column)
        {
            return GetOffsetFromPosition(
                GetLineMap(text),
                line,
                column);
        }

        internal static int GetOffsetFromPosition(int[] lineStartMap, int line, int column)
        {
            return lineStartMap[line - 1] + column;
        }

        internal static IScriptExtent ReduceBoundsWhitespace(IScriptExtent extent)
        {
            int startColumn = extent.StartColumnNumber;
            while (char.IsWhiteSpace(extent.StartScriptPosition.Line[startColumn - 1]))
            {
                if (startColumn == extent.StartScriptPosition.Line.Length)
                {
                    if (extent.StartScriptPosition.Line.EndsWith(System.Environment.NewLine))
                    {
                        startColumn -= System.Environment.NewLine.Length;
                    }

                    break;
                }

                startColumn++;
            }

            int endColumn = extent.EndColumnNumber;
            while (endColumn > 1
                && char.IsWhiteSpace(extent.EndScriptPosition.Line[endColumn - 2]))
            {
                if (endColumn == 1)
                {
                    break;
                }

                endColumn--;
            }

            if (startColumn == extent.StartColumnNumber && endColumn == extent.EndColumnNumber)
            {
                return extent;
            }

            return NewScriptExtent(
                extent,
                extent.StartOffset + startColumn - extent.StartColumnNumber,
                extent.EndOffset - (extent.EndColumnNumber - endColumn));
        }

        internal static IScriptExtent GetFullLines(IScriptExtent extent)
        {
            if (extent.StartColumnNumber == 1 &&
                extent.EndLineNumber == extent.EndScriptPosition.Line.Length)
            {
                return extent;
            }

            return NewScriptExtent(
                extent,
                GetLineStartOffset(extent.StartScriptPosition),
                GetLineEndOffset(extent.EndScriptPosition));
        }

        internal static int GetLineEndOffset(IScriptPosition position)
        {
            int lineLength = position.Line.EndsWith(System.Environment.NewLine)
                ? position.Line.Length - System.Environment.NewLine.Length
                : position.Line.Length;

            return lineLength < position.ColumnNumber
                ? position.Offset
                : lineLength - position.ColumnNumber + position.Offset + 1;
        }

        internal static int GetLineStartOffset(IScriptPosition position)
        {
            return position.Offset - position.ColumnNumber + 1;
        }

        internal static int GetLineTextStartOffset(IScriptPosition position)
        {
            return
                position.Line.ToCharArray().TakeWhile(c => char.IsWhiteSpace(c)).Count()
                - position.ColumnNumber
                + position.Offset
                + 1;
        }
    }
}
