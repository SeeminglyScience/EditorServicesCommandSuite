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
