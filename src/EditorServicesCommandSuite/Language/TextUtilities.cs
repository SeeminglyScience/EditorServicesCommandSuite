using System;
using System.Collections.Generic;
using System.Linq;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Language
{
    internal static class TextUtilities
    {
        internal static IEnumerable<string> NormalizeIndent(IEnumerable<string> lines, string tabString)
        {
            int smallestIndent = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Min(line => DetectIndent(line, tabString));

            if (smallestIndent == 0)
            {
                return lines;
            }

            return lines.Select(
                line => string.IsNullOrWhiteSpace(line)
                    ? string.Empty
                    : line.Substring(smallestIndent * tabString.Length));
        }

        internal static int DetectIndent(string text, string tabString)
        {
            if (text.Length < tabString.Length)
            {
                return 0;
            }

            var indent = 0;
            var index = 0;
            while (text.Length - index >= tabString.Length
                && text
                    .Substring(index, tabString.Length)
                    .Equals(tabString, StringComparison.Ordinal))
            {
                index = index + tabString.Length;
                indent++;
            }

            return indent;
        }

        internal static IEnumerable<string> GetLines(string text)
        {
            return GetLines(text, Settings.NewLine);
        }

        internal static IEnumerable<string> GetLines(string text, string newLine)
        {
            int lastIndex = text.IndexOf(newLine);
            if (lastIndex == -1)
            {
                yield return text;
                yield break;
            }

            yield return text.Substring(0, lastIndex);
            while (true)
            {
                int newLastIndex = text.IndexOf(newLine, lastIndex + newLine.Length);
                if (newLastIndex == -1)
                {
                    if (text.Length - lastIndex == newLine.Length)
                    {
                        break;
                    }

                    yield return text.Substring(
                        lastIndex + newLine.Length, text.Length - (lastIndex + newLine.Length));
                    break;
                }

                yield return text.Substring(
                    lastIndex + newLine.Length,
                    newLastIndex - (lastIndex + newLine.Length));
                lastIndex = newLastIndex;
            }
        }
    }
}
