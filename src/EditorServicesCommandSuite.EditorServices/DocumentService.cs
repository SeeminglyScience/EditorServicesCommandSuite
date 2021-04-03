using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices.Extensions;
using Microsoft.PowerShell.EditorServices.Extensions.Services;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class DocumentService : IDocumentEditProcessor
    {
        private readonly IWorkspaceService _workspace;

        private readonly IEditorContextService _context;

        private readonly ILanguageServerService _messages;

        internal DocumentService(
            IWorkspaceService workspace,
            IEditorContextService context,
            ILanguageServerService messages)
        {
            _workspace = workspace;
            _context = context;
            _messages = messages;
        }

        public async Task WriteDocumentEditsAsync(IEnumerable<DocumentEdit> edits, CancellationToken cancellationToken)
        {
            ILspCurrentFileContext context = await GetClientContext()
                .ConfigureAwait(false);

            // Order by empty file names first so the first group processed is the current file.
            IOrderedEnumerable<IGrouping<Uri, DocumentEdit>> orderedGroups = edits
                .GroupBy(e => e.Uri)
                .OrderByDescending(g => g.Key == null);

            var workspaceChanges = new List<WorkspaceEditDocumentChange>();
            foreach (IGrouping<Uri, DocumentEdit> editGroup in orderedGroups)
            {
                IEditorScriptFile scriptFile;
                try
                {
                    scriptFile = _workspace.GetFile(editGroup.Key ?? context.Uri);
                }
                catch (FileNotFoundException)
                {
                    scriptFile = await CreateNewFile(context, editGroup.Key, cancellationToken)
                        .ConfigureAwait(false);
                }

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

                var versionedIdentifier = new OptionalVersionedTextDocumentIdentifier
                {
                    Uri = DocumentUri.From(editGroup.Key ?? context.Uri),
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
            await _messages.ApplyEdit(
                new ApplyWorkspaceEditParams { Edit = workspaceEdit })
                .ConfigureAwait(false);
        }

        internal static Position ToServerPosition(LspPosition position)
        {
            return new Position()
            {
                Line = position.Line - 1,
                Character = position.Character - 1,
            };
        }

        private async Task<IEditorScriptFile> CreateNewFile(
            ILspCurrentFileContext context,
            Uri uri,
            CancellationToken cancellationToken)
        {
            // Path parameter doesn't actually do anything currently. The new file will be untitled.
            await _context.OpenNewUntitledFileAsync().ConfigureAwait(false);

            ILspCurrentFileContext newContext;
            while (true)
            {
                newContext = await GetClientContext().ConfigureAwait(false);
                if (newContext.Uri != context.Uri)
                {
                    break;
                }

                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
            }

            IEditorScriptFile scriptFile = _workspace.GetFile(newContext.Uri);
            await _context.SaveFileAsync(scriptFile.Uri, uri).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            return _workspace.GetFile(uri);
        }

        private Task<ILspCurrentFileContext> GetClientContext() => _context.GetCurrentLspFileContextAsync();
    }
}
