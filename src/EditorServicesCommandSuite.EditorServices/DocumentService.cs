using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices;
using Microsoft.PowerShell.EditorServices.Protocol.LanguageServer;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class DocumentService : IDocumentEditProcessor
    {
        private readonly EditorSession _editorSession;

        private readonly MessageService _messages;

        internal DocumentService(EditorSession editorSession, MessageService messages)
        {
            _editorSession = editorSession;
            _messages = messages;
        }

        public async Task WriteDocumentEditsAsync(IEnumerable<DocumentEdit> edits)
        {
            var context = await _messages.SendRequestAsync(
                GetEditorContextRequest.Type,
                new GetEditorContextRequest(),
                waitForResponse: true);

            var scriptFile = _editorSession.Workspace.GetFile(context.CurrentFilePath);
            foreach (var edit in edits.OrderByDescending(edit => edit.StartOffset))
            {
                var request = new InsertTextRequest()
                {
                    FilePath = scriptFile.ClientFilePath,
                    InsertText = edit.NewValue,
                    InsertRange = new Range()
                    {
                        Start = ToServerPosition(scriptFile.GetPositionAtOffset((int)edit.StartOffset)),
                        End = ToServerPosition(scriptFile.GetPositionAtOffset((int)edit.EndOffset)),
                    },
                };

                await _messages.SendRequestAsync(
                    InsertTextRequest.Type,
                    request,
                    waitForResponse: true);

                await Task.Delay(TimeSpan.FromMilliseconds(50));
            }
        }

        internal static Position ToServerPosition(BufferPosition position)
        {
            return new Position()
            {
                Line = position.Line - 1,
                Character = position.Column - 1,
            };
        }
    }
}
