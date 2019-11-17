using System.Threading;
using System.Threading.Tasks;

using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices.Services.PowerShellContext;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class EditorServicesNavigationService : NavigationService, INavigationSupportsOpenDocument
    {
        private readonly MessageService _messages;

        internal EditorServicesNavigationService(MessageService messages)
        {
            _messages = messages;
        }

        public void OpenDocument(string path)
        {
            OpenDocument(path, CancellationToken.None);
        }

        public void OpenDocument(string path, CancellationToken cancellationToken)
        {
            _messages.SendRequest(
                Messages.OpenFile,
                new OpenFileDetails()
                {
                    FilePath = path,
                });
        }

        public Task OpenDocumentAsync(string path)
        {
            return OpenDocumentAsync(path, CancellationToken.None);
        }

        public Task OpenDocumentAsync(string path, CancellationToken cancellationToken)
        {
            return _messages.SendRequestAsync(
                Messages.OpenFile,
                new OpenFileDetails()
                {
                    FilePath = path,
                });
        }

        public override void SetCursorPosition(int line, int column, CancellationToken cancellationToken)
        {
            _messages.SendRequest(
                Messages.SetSelection,
                new SetSelectionRequest()
                {
                    SelectionRange = new Range()
                    {
                        Start = new Position()
                        {
                            Line = line - 1,
                            Character = column - 1,
                        },
                        End = new Position()
                        {
                            Line = line - 1,
                            Character = column - 1,
                        },
                    },
                });
        }

        public override async Task SetCursorPositionAsync(int line, int column, CancellationToken cancellationToken)
        {
            await _messages.SendRequestAsync(
                Messages.SetSelection,
                new SetSelectionRequest()
                {
                    SelectionRange = new Range()
                    {
                        Start = new Position()
                        {
                            Line = line - 1,
                            Character = column - 1,
                        },
                        End = new Position()
                        {
                            Line = line - 1,
                            Character = column - 1,
                        },
                    },
                })
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public override void SetSelection(
            int startLine,
            int startColumn,
            int endLine,
            int endColumn,
            CancellationToken cancellationToken)
        {
            _messages.SendRequest(
                Messages.SetSelection,
                new SetSelectionRequest()
                {
                    SelectionRange = new Range()
                    {
                        Start = new Position()
                        {
                            Line = startLine - 1,
                            Character = startColumn - 1,
                        },
                        End = new Position()
                        {
                            Line = endLine - 1,
                            Character = endColumn - 1,
                        },
                    },
                });
        }

        public override async Task SetSelectionAsync(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken)
        {
            await _messages.SendRequestAsync(
                Messages.SetSelection,
                new SetSelectionRequest()
                {
                    SelectionRange = new Range()
                    {
                        Start = new Position()
                        {
                            Line = startLine - 1,
                            Character = startColumn - 1,
                        },
                        End = new Position()
                        {
                            Line = endLine - 1,
                            Character = endColumn - 1,
                        },
                    },
                }).ConfigureAwait(false);
        }
    }
}
