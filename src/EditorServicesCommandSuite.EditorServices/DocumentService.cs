using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices.Handlers;
using Microsoft.PowerShell.EditorServices.Services.PowerShellContext;
using Microsoft.PowerShell.EditorServices.Services.TextDocument;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

using PSWorkspaceService = Microsoft.PowerShell.EditorServices.Services.WorkspaceService;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class DocumentService : IDocumentEditProcessor
    {
        private readonly PSWorkspaceService _workspace;

        private readonly MessageService _messages;

        internal DocumentService(PSWorkspaceService workspace, MessageService messages)
        {
            _workspace = workspace;
            _messages = messages;
        }

        public async Task WriteDocumentEditsAsync(IEnumerable<DocumentEdit> edits, CancellationToken cancellationToken)
        {
            ClientEditorContext context = await GetClientContext()
                .ConfigureAwait(false);

            // Order by empty file names first so the first group processed is the current file.
            IOrderedEnumerable<IGrouping<string, DocumentEdit>> orderedGroups = edits
                .GroupBy(e => e.FileName)
                .OrderByDescending(g => string.IsNullOrEmpty(g.Key));

            var workspaceChanges = new List<WorkspaceEditDocumentChange>();
            foreach (IGrouping<string, DocumentEdit> editGroup in orderedGroups)
            {
                ScriptFile scriptFile;
                try
                {
                    scriptFile = _workspace.GetFile(
                        string.IsNullOrEmpty(editGroup.Key) ? context.CurrentFilePath : editGroup.Key);
                }
                catch (FileNotFoundException)
                {
                    scriptFile = await CreateNewFile(context, editGroup.Key, cancellationToken)
                        .ConfigureAwait(false);
                }

                // ScriptFile.ClientFilePath isn't always a URI.
                string clientFilePath = DocumentHelpers.GetPathAsClientPath(scriptFile.ClientFilePath);
                var textEdits = new List<TextEdit>();
                foreach (DocumentEdit edit in editGroup)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var range = new Range
                    {
                        Start = ToServerPosition(scriptFile.GetPositionAtOffset((int)edit.StartOffset)),
                        End = ToServerPosition(scriptFile.GetPositionAtOffset((int)edit.EndOffset)),
                    };

                    var textEdit = new TextEdit
                    {
                        NewText = edit.NewValue,
                        Range = range,
                    };

                    textEdits.Add(textEdit);
                }

                var versionedIdentifier = new VersionedTextDocumentIdentifier
                {
                    Uri = new Uri(clientFilePath),
                    Version = default,
                };

                var textDocumentEdit = new TextDocumentEdit
                {
                    Edits = textEdits,
                    TextDocument = versionedIdentifier,
                };

                workspaceChanges.Add(new WorkspaceEditDocumentChange(textDocumentEdit));
            }

            var workspaceEdit = new WorkspaceEdit { DocumentChanges = workspaceChanges };
            await _messages.Sender.Workspace.ApplyEdit(
                new ApplyWorkspaceEditParams { Edit = workspaceEdit })
                .ConfigureAwait(false);
        }

        internal static Position ToServerPosition(BufferPosition position)
        {
            return new Position()
            {
                Line = position.Line - 1,
                Character = position.Column - 1,
            };
        }

        private async Task<ScriptFile> CreateNewFile(
            ClientEditorContext context,
            string path,
            CancellationToken cancellationToken)
        {
            // Path parameter doesn't actually do anything currently. The new file will be untitled.
            await _messages.SendRequestAsync(Messages.NewFile, path)
                .ConfigureAwait(false);

            ClientEditorContext newContext;
            while (true)
            {
                newContext = await GetClientContext().ConfigureAwait(false);
                if (!newContext.CurrentFilePath.Equals(context.CurrentFilePath, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
            }

            ScriptFile scriptFile = _workspace.GetFile(newContext.CurrentFilePath);
            await _messages.SendRequestAsync(
                Messages.SaveFile,
                new SaveFileDetails()
                {
                    FilePath = scriptFile.ClientFilePath,
                    NewPath = path,
                }).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            return _workspace.GetFile(path);
        }

        private async Task<ClientEditorContext> GetClientContext()
        {
            return await _messages.SendRequestAsync(
                Messages.GetEditorContext,
                new GetEditorContextRequest())
                .ConfigureAwait(false);
        }
    }
}
