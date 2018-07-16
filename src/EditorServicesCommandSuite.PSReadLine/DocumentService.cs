using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal class DocumentService : IDocumentEditProcessor
    {
        public Task WriteDocumentEditsAsync(IEnumerable<DocumentEdit> edits)
        {
            // Add the current buffer to history in case PSRL can't undo successfully.
            PSConsoleReadLine.GetBufferState(out string input, out _);
            PSConsoleReadLine.AddToHistory(input);
            foreach (var edit in edits.OrderByDescending(edit => edit.StartOffset))
            {
                PSConsoleReadLine.Replace(
                    (int)edit.StartOffset,
                    (int)(edit.EndOffset - edit.StartOffset),
                    edit.NewValue.Replace("\r", string.Empty));
            }

            return Task.CompletedTask;
        }
    }
}
