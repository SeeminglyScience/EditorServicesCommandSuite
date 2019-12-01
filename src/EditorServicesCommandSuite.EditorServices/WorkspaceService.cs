using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;
using Microsoft.PowerShell.EditorServices.Services.TextDocument;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using PSWorkspaceService = Microsoft.PowerShell.EditorServices.Services.WorkspaceService;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class WorkspaceService : WorkspaceContext
    {
        private readonly PSWorkspaceService _workspace;

        private readonly MessageService _messages;

        internal WorkspaceService(EngineIntrinsics engine, PSWorkspaceService workspace, MessageService messages)
            : base(engine)
        {
            _workspace = workspace;
            _messages = messages;
        }

        public override async Task DeleteFileAsync(string path, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new PSArgumentNullException(path);
            }

            string fileUri = DocumentHelpers.GetPathAsClientPath(path);
            await _messages.Sender.Workspace.ApplyEdit(
                new ApplyWorkspaceEditParams()
                {
                    Edit = new WorkspaceEdit()
                    {
                        DocumentChanges = new[]
                        {
                            new WorkspaceEditDocumentChange(
                                new DeleteFile()
                                {
                                    Uri = fileUri,
                                    Options = new DeleteFileOptions()
                                    {
                                        IgnoreIfNotExists = true,
                                        Recursive = false,
                                    },
                                }),
                        },
                    },
                }).ConfigureAwait(false);
        }

        public override string GetWorkspacePath() => _workspace.WorkspacePath;

        public override bool IsUntitledWorkspace()
        {
            return string.IsNullOrEmpty(_workspace.WorkspacePath);
        }

        public override async Task MoveFileAsync(string path, string destination, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new PSArgumentNullException(path);
            }

            if (string.IsNullOrWhiteSpace(destination))
            {
                throw new PSArgumentNullException(destination);
            }

            destination = Directory.Exists(destination)
                ? destination
                : Path.Combine(destination, Path.GetFileName(path));

            string pathUri = DocumentHelpers.GetPathAsClientPath(path);
            string destinationUri = DocumentHelpers.GetPathAsClientPath(destination);
            await RenameFileImplAsync(pathUri, destinationUri).ConfigureAwait(false);
        }

        public override async Task RenameFileAsync(string path, string newName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new PSArgumentNullException(path);
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                throw new PSArgumentNullException(newName);
            }

            string sourceExtension = Path.GetExtension(path);
            if (!Path.GetExtension(newName).Equals(sourceExtension, PathUtils.PathComparision))
            {
                newName = Path.ChangeExtension(newName, sourceExtension);
            }

            newName = Path.Combine(
                Path.GetDirectoryName(path),
                newName);

            await RenameFileImplAsync(
                DocumentHelpers.GetPathAsClientPath(path),
                DocumentHelpers.GetPathAsClientPath(newName))
                .ConfigureAwait(false);
        }

        protected override Tuple<ScriptBlockAst, Token[]> GetFileContext(string path)
        {
            ScriptFile scriptFile = _workspace.GetFile(path);
            return Tuple.Create(scriptFile.ScriptAst, scriptFile.ScriptTokens);
        }

        private async Task RenameFileImplAsync(string pathUri, string destinationUri)
        {
            await _messages.Sender.Workspace.ApplyEdit(
                new ApplyWorkspaceEditParams()
                {
                    Edit = new WorkspaceEdit()
                    {
                        DocumentChanges = new[]
                        {
                            new WorkspaceEditDocumentChange(
                                new RenameFile()
                                {
                                    OldUri = pathUri,
                                    NewUri = destinationUri,
                                    Options = new RenameFileOptions()
                                    {
                                        IgnoreIfExists = true,
                                        Overwrite = false,
                                    },
                                }),
                        },
                    },
                }).ConfigureAwait(false);
        }
    }
}
