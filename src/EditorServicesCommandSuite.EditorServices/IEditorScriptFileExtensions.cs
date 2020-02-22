using System;
using Microsoft.PowerShell.EditorServices.Extensions.Services;

namespace EditorServicesCommandSuite.EditorServices
{
    internal static class IEditorScriptFileExtensions
    {
        public static LspPosition GetPositionAtOffset(this IEditorScriptFile scriptFile, int offset)
            => (LspPosition)GetRangeBetweenOffsets(scriptFile, offset, offset).Start;

        public static LspRange GetRangeBetweenOffsets(
            this IEditorScriptFile scriptFile,
            int startOffset,
            int endOffset)
        {
            bool foundStart = false;
            int currentOffset = 0;
            int searchedOffset = startOffset;

            var startPosition = new LspPosition(0, 0);
            LspPosition endPosition = startPosition;

            int line = 0;
            int totalLineCount = scriptFile.Lines.Count;
            while (line < totalLineCount)
            {
                if (searchedOffset <= currentOffset + scriptFile.Lines[line].Length)
                {
                    int column = searchedOffset - currentOffset;

                    // Have we already found the start position?
                    if (foundStart)
                    {
                        // Assign the end position and end the search
                        endPosition = new LspPosition(line + 1, column + 1);
                        break;
                    }
                    else
                    {
                        startPosition = new LspPosition(line + 1, column + 1);

                        // Do we only need to find the start position?
                        if (startOffset == endOffset)
                        {
                            endPosition = startPosition;
                            break;
                        }
                        else
                        {
                            // Since the end offset can be on the same line,
                            // skip the line increment and continue searching
                            // for the end position
                            foundStart = true;
                            searchedOffset = endOffset;
                            continue;
                        }
                    }
                }

                // Increase the current offset and include newline length
                currentOffset += scriptFile.Lines[line].Length + Environment.NewLine.Length;
                line++;
            }

            return new LspRange(startPosition, endPosition);
        }
    }
}
