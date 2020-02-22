using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;

using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;
using Microsoft.PowerShell.EditorServices.Extensions.Services;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class WorkspaceService : WorkspaceContext
    {
        private readonly IWorkspaceService _workspace;

        private readonly ILanguageServerService _messages;

        internal WorkspaceService(EngineIntrinsics engine, IWorkspaceService workspace, ILanguageServerService messages)
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
            await _messages.ApplyEdit(
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
            IEditorScriptFile scriptFile = _workspace.GetFile(new Uri(path));
            return Tuple.Create(scriptFile.Ast, scriptFile.Tokens.ToArray());
        }

        private async Task RenameFileImplAsync(string pathUri, string destinationUri)
        {
            var editParams = new ApplyWorkspaceEditParams()
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
            };

            await _messages.ApplyEdit(editParams).ConfigureAwait(false);
        }
    }
}
