using System;
using System.Collections.Generic;

namespace EditorServicesCommandSuite.Language
{
    internal static class TextUtilities
    {
        /// <summary>
        /// Represents a delegate that accepts a partial string in the form
        /// of a <see cref="ReadOnlySpan{T}" /> of type <see cref="char" />.
        /// </summary>
        /// <param name="text">The string chunk to process.</param>
        public delegate void StringChunkHandler(ReadOnlySpan<char> text);

        /// <summary>
        /// Invokes the specified delegate for every line in the specified text.
        /// </summary>
        /// <param name="lineHandler">The action to perform for each line.</param>
        /// <param name="text">The source text containing multiple lines.</param>
        /// <param name="newLine">The string the represents a new line.</param>
        public static void ForEachLine(
            StringChunkHandler lineHandler,
            string text,
            string newLine)
        {
            ForEachLine(
                lineHandler,
                text.AsSpan(),
                newLine.AsSpan());
        }

        /// <summary>
        /// Invokes the specified delegate for every line in the specified text.
        /// </summary>
        /// <param name="lineHandler">The action to perform for each line.</param>
        /// <param name="text">The source text containing multiple lines.</param>
        /// <param name="newLine">The string the represents a new line.</param>
        public static void ForEachLine(
            StringChunkHandler lineHandler,
            ReadOnlySpan<char> text,
            ReadOnlySpan<char> newLine)
        {
            ForEachLineImpl(
                lineHandler ?? throw new ArgumentNullException(nameof(lineHandler)),
                separator: null,
                text,
                newLine);
        }

        /// <summary>
        /// Invokes the specified delegate for every line in the specified text.
        /// </summary>
        /// <param name="lineHandler">The action to perform for each line.</param>
        /// <param name="separator">The action to perform between each line.</param>
        /// <param name="text">The source text containing multiple lines.</param>
        /// <param name="newLine">The string the represents a new line.</param>
        public static void ForEachLine(
            StringChunkHandler lineHandler,
            Action separator,
            string text,
            string newLine)
        {
            ForEachLine(
                lineHandler,
                separator,
                text.AsSpan(),
                newLine.AsSpan());
        }

        /// <summary>
        /// Invokes the specified delegate for every line in the specified text.
        /// </summary>
        /// <param name="lineHandler">The action to perform for each line.</param>
        /// <param name="separator">The action to perform between each line.</param>
        /// <param name="text">The source text containing multiple lines.</param>
        /// <param name="newLine">The string the represents a new line.</param>
        public static void ForEachLine(
            StringChunkHandler lineHandler,
            Action separator,
            ReadOnlySpan<char> text,
            ReadOnlySpan<char> newLine)
        {
            ForEachLineImpl(
                lineHandler ?? throw new ArgumentNullException(nameof(lineHandler)),
                separator ?? throw new ArgumentNullException(nameof(separator)),
                text,
                newLine);
        }

        /// <summary>
        /// Invokes the specified delegate for every line in the specified text.
        /// Extra indentation will be removed, however actual indentation levels
        /// will be perserved.
        /// </summary>
        /// <param name="handler">The action to perform for each line.</param>
        /// <param name="text">The source text containing multiple lines.</param>
        /// <param name="newLine">The string the represents a new line.</param>
        /// <param name="tabString">The string the respresents a tab character.</param>
        /// <param name="ignoreFirstLine">
        /// A value indictating whether the first line should be included when determining
        /// the smallest indent level.
        /// </param>
        public static void ForEachIndentNormalizedLine(
            StringChunkHandler handler,
            string text,
            string newLine,
            string tabString,
            bool ignoreFirstLine = false)
        {
            ForEachIndentNormalizedLine(
                handler,
                text.AsSpan(),
                newLine.AsSpan(),
                tabString.AsSpan(),
                ignoreFirstLine);
        }

        /// <summary>
        /// Invokes the specified delegate for every line in the specified text.
        /// Extra indentation will be removed, however actual indentation levels
        /// will be perserved.
        /// </summary>
        /// <param name="handler">The action to perform for each line.</param>
        /// <param name="text">The source text containing multiple lines.</param>
        /// <param name="newLine">The string the represents a new line.</param>
        /// <param name="tabString">The string the respresents a tab character.</param>
        /// <param name="ignoreFirstLine">
        /// A value indictating whether the first line should be included when determining
        /// the smallest indent level.
        /// </param>
        public static void ForEachIndentNormalizedLine(
            StringChunkHandler handler,
            ReadOnlySpan<char> text,
            ReadOnlySpan<char> newLine,
            ReadOnlySpan<char> tabString,
            bool ignoreFirstLine = false)
        {
            ForEachIndentNormalizedLineImpl(
                handler ?? throw new ArgumentNullException(nameof(handler)),
                separator: null,
                text,
                newLine,
                tabString,
                ignoreFirstLine);
        }

        /// <summary>
        /// Invokes the specified delegate for every line in the specified text.
        /// Extra indentation will be removed, however actual indentation levels
        /// will be perserved.
        /// </summary>
        /// <param name="handler">The action to perform for each line.</param>
        /// <param name="separator">The action to perform between each line.</param>
        /// <param name="text">The source text containing multiple lines.</param>
        /// <param name="newLine">The string the represents a new line.</param>
        /// <param name="tabString">The string the respresents a tab character.</param>
        /// <param name="ignoreFirstLine">
        /// A value indictating whether the first line should be included when determining
        /// the smallest indent level.
        /// </param>
        public static void ForEachIndentNormalizedLine(
            StringChunkHandler handler,
            Action separator,
            string text,
            string newLine,
            string tabString,
            bool ignoreFirstLine = false)
        {
            ForEachIndentNormalizedLine(
                handler,
                separator,
                text.AsSpan(),
                newLine.AsSpan(),
                tabString.AsSpan(),
                ignoreFirstLine);
        }

        /// <summary>
        /// Invokes the specified delegate for every line in the specified text.
        /// Extra indentation will be removed, however actual indentation levels
        /// will be perserved.
        /// </summary>
        /// <param name="handler">The action to perform for each line.</param>
        /// <param name="separator">The action to perform between each line.</param>
        /// <param name="text">The source text containing multiple lines.</param>
        /// <param name="newLine">The string the represents a new line.</param>
        /// <param name="tabString">The string the respresents a tab character.</param>
        /// <param name="ignoreFirstLine">
        /// A value indictating whether the first line should be included when determining
        /// the smallest indent level.
        /// </param>
        public static void ForEachIndentNormalizedLine(
            StringChunkHandler handler,
            Action separator,
            ReadOnlySpan<char> text,
            ReadOnlySpan<char> newLine,
            ReadOnlySpan<char> tabString,
            bool ignoreFirstLine = false)
        {
            ForEachIndentNormalizedLineImpl(
                handler ?? throw new ArgumentNullException(nameof(handler)),
                separator ?? throw new ArgumentNullException(nameof(separator)),
                text,
                newLine,
                tabString,
                ignoreFirstLine);
        }

        /// <summary>
        /// Determine the indentation level of the specified text.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <param name="tabString">The string that represents a tab character.</param>
        /// <returns>
        /// A number representing the amount of times that the specified tab
        /// string appears at the start of the specified text.
        /// </returns>
        /// <remarks>
        /// It is assumed that the provided string does not contain
        /// any new line characters.
        /// </remarks>
        public static int DetectIndent(string text, string tabString)
        {
            return DetectIndent(text.AsSpan(), tabString.AsSpan());
        }

        /// <summary>
        /// Determine the indentation level of the specified text.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <param name="tabString">The string that represents a tab character.</param>
        /// <returns>
        /// A number representing the amount of times that the specified tab
        /// string appears at the start of the specified text.
        /// </returns>
        /// <remarks>
        /// It is assumed that the provided string does not contain
        /// any new line characters.
        /// </remarks>
        public static int DetectIndent(ReadOnlySpan<char> text, ReadOnlySpan<char> tabString)
        {
            if (text.Length < tabString.Length)
            {
                return 0;
            }

            char firstTabChar = tabString[0];
            int indent = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != firstTabChar)
                {
                    break;
                }

                bool isTabString = true;
                for (int j = 1; j < tabString.Length; j++)
                {
                    if (i + j >= text.Length || text[i + j] != tabString[j])
                    {
                        isTabString = false;
                        break;
                    }
                }

                if (!isTabString)
                {
                    break;
                }

                i += tabString.Length - 1;
                indent++;
            }

            return indent;
        }

        /// <summary>
        /// Invokes the specified delegate for every line in the specified text.
        /// </summary>
        /// <param name="lineHandler">The action to perform for each line.</param>
        /// <param name="separator">The action to perform between each line.</param>
        /// <param name="text">The source text containing multiple lines.</param>
        /// <param name="newLine">The string the represents a new line.</param>
        private static void ForEachLineImpl(
            StringChunkHandler lineHandler,
            Action separator,
            ReadOnlySpan<char> text,
            ReadOnlySpan<char> newLine)
        {
            ReadOnlySpan<TextSegment> lineMap = GetLineMap(text, newLine);
            for (int i = 0; i < lineMap.Length; i++)
            {
                TextSegment segment = lineMap[i];
                if (segment.Length == 0)
                {
                    lineHandler(ReadOnlySpan<char>.Empty);
                }
                else
                {
                    lineHandler(text.Slice(segment.Offset, segment.Length));
                }

                if (separator != null && i + 1 < lineMap.Length)
                {
                    separator();
                }
            }
        }

        /// <summary>
        /// Invokes the specified delegate for every line in the specified text.
        /// Extra indentation will be removed, however actual indentation levels
        /// will be perserved.
        /// </summary>
        /// <param name="handler">The action to perform for each line.</param>
        /// <param name="separator">The action to perform between each line.</param>
        /// <param name="text">The source text containing multiple lines.</param>
        /// <param name="newLine">The string the represents a new line.</param>
        /// <param name="tabString">The string the respresents a tab character.</param>
        /// <param name="ignoreFirstLine">
        /// A value indictating whether the first line should be included when determining
        /// the smallest indent level.
        /// </param>
        private static void ForEachIndentNormalizedLineImpl(
            StringChunkHandler handler,
            Action separator,
            ReadOnlySpan<char> text,
            ReadOnlySpan<char> newLine,
            ReadOnlySpan<char> tabString,
            bool ignoreFirstLine = false)
        {
            void ProcessLine(ReadOnlySpan<char> line, int lineIndex, int mapLength)
            {
                handler(line);
                if (separator == null || lineIndex + 1 == mapLength)
                {
                    return;
                }

                separator();
            }

            // Skip the first line when determining smallest indent if ignoreFirstLine is set.
            int i = ignoreFirstLine ? 1 : 0;
            int smallestIndent = -1;
            ReadOnlySpan<TextSegment> lineMap = GetLineMap(text, newLine);
            for (; i < lineMap.Length; i++)
            {
                TextSegment segment = lineMap[i];

                // Don't count empty lines in the smallest indent determination. Ideally
                // empty lines would not have any white space.
                if (segment.Length == 0)
                {
                    continue;
                }

                ReadOnlySpan<char> line = text.Slice(segment.Offset, segment.Length);
                int indent = DetectIndent(line, tabString);
                if (smallestIndent == -1 || indent < smallestIndent)
                {
                    smallestIndent = indent;
                }
            }

            // There's either no lines or at least one with no indentation, so return as is.
            if (smallestIndent < 1)
            {
                for (i = 0; i < lineMap.Length; i++)
                {
                    TextSegment segement = lineMap[i];
                    ProcessLine(text.Slice(segement.Offset, segement.Length), i, lineMap.Length);
                }

                return;
            }

            // We don't want to edit the first line in any way if ignoreFirstLine is specified.
            if (ignoreFirstLine)
            {
                ProcessLine(text.Slice(lineMap[0].Offset, lineMap[0].Length), 0, lineMap.Length);
                i = 1;
            }
            else
            {
                i = 0;
            }

            int tabStringLength = tabString.Length;
            for (; i < lineMap.Length; i++)
            {
                TextSegment segment = lineMap[i];
                if (segment.Length == 0)
                {
                    ProcessLine(ReadOnlySpan<char>.Empty, i, lineMap.Length);
                    continue;
                }

                // Trim lines so indent level is perserved but they can be written
                // using the indent state of a writer.
                int trimLength = smallestIndent * tabStringLength;
                ProcessLine(
                    text.Slice(segment.Offset + trimLength, segment.Length - trimLength),
                    i,
                    lineMap.Length);
            }
        }

        /// <summary>
        /// Get a map of line start offsets and line length.
        /// </summary>
        /// <param name="text">The source text.</param>
        /// <param name="newLine">The string that represents a new line.</param>
        /// <returns>A map of line start offsets and line length.</returns>
        /// <remarks>
        /// This method is heavily based on the code for PositionHelper in the
        /// PowerShell Core repository.
        /// </remarks>
        private static ReadOnlySpan<TextSegment> GetLineMap(ReadOnlySpan<char> text, ReadOnlySpan<char> newLine)
        {
            int newLineLength = newLine.Length;
            var lineStartMap = new List<TextSegment>(100);
            char newLineStart = newLine[0];
            int lastLineStart = 0;
            for (int i = 0; i < text.Length; ++i)
            {
                char c = text[i];
                if (c != newLineStart)
                {
                    continue;
                }

                bool isNewLine = true;
                if (i + newLineLength >= text.Length)
                {
                    break;
                }

                for (int j = 1; j < newLine.Length; j++)
                {
                    if (text[i + j] != newLine[j])
                    {
                        isNewLine = false;
                        break;
                    }
                }

                if (!isNewLine)
                {
                    continue;
                }

                lineStartMap.Add(new TextSegment(lastLineStart, i - lastLineStart));
                lastLineStart = i + newLineLength;
                i += newLineLength - 1;
            }

            if (lastLineStart < text.Length)
            {
                lineStartMap.Add(new TextSegment(lastLineStart, text.Length - lastLineStart));
            }

            return lineStartMap.ToArray();
        }

        /// <summary>
        /// Represents the location of a line within a string.
        /// </summary>
        public readonly struct TextSegment
        {
            /// <summary>
            /// Indicates the zero based offset of the starting character in the line.
            /// </summary>
            public readonly int Offset;

            /// <summary>
            /// Indicates the length of the line excluding new line characters.
            /// </summary>
            public readonly int Length;

            public TextSegment(int offset, int length)
            {
                Offset = offset;
                Length = length;
            }
        }
    }
}
