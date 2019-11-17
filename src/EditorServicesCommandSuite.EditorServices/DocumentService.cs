using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices.Handlers;
using Microsoft.PowerShell.EditorServices.Services.PowerShellContext;
using Microsoft.PowerShell.EditorServices.Services.TextDocument;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

using PSWorkspaceService = Microsoft.PowerShell.EditorServices.Services.WorkspaceService;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class DocumentService : IDocumentEditProcessor
    {
        private const string FileUriPrefix = "file:///";

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
                string clientFilePath = GetPathAsClientPath(scriptFile.ClientFilePath);
                foreach (var edit in editGroup.OrderByDescending(edit => edit.StartOffset))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var request = new InsertTextRequest()
                    {
                        FilePath = clientFilePath,
                        InsertText = edit.NewValue,
                        InsertRange = new Range()
                        {
                            Start = ToServerPosition(scriptFile.GetPositionAtOffset((int)edit.StartOffset)),
                            End = ToServerPosition(scriptFile.GetPositionAtOffset((int)edit.EndOffset)),
                        },
                    };

                    await _messages
                        .SendRequestAsync(Messages.InsertText, request)
                        .ConfigureAwait(false);

                    await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken).ConfigureAwait(false);
                }
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

        private static string GetPathAsClientPath(string path)
        {
            if (path.StartsWith("untitled:", StringComparison.Ordinal))
            {
                return path;
            }

            Debug.Assert(
                !string.IsNullOrWhiteSpace(path),
                "Caller should verify path is valid");

            if (path.StartsWith("file:///", StringComparison.Ordinal))
            {
                return path;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new Uri(path).AbsoluteUri;
            }

            // VSCode file URIs on Windows need the drive letter lowercase, and the colon
            // URI encoded. System.Uri won't do that, so we manually create the URI.
            var newUri = new StringBuilder(HttpUtility.UrlPathEncode(path));
            int colonIndex = path.IndexOf(Symbols.Colon);
            for (var i = colonIndex - 1; i >= 0; i--)
            {
                newUri.Remove(i, 1);
                newUri.Insert(i, char.ToLowerInvariant(path[i]));
            }

            return newUri
                .Remove(colonIndex, 1)
                .Insert(colonIndex, "%3A")
                .Replace(Symbols.Backslash, Symbols.ForwardSlash)
                .Insert(0, FileUriPrefix)
                .ToString();
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
