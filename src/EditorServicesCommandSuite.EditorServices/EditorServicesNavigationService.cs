using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices.Protocol.LanguageServer;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class EditorServicesNavigationService : NavigationService, INavigationSupportsOpenDocument
    {
        private readonly MessageService _messageService;

        internal EditorServicesNavigationService(MessageService messageService)
        {
            _messageService = messageService;
        }

        public void OpenDocument(string path)
        {
            OpenDocument(path, CancellationToken.None);
        }

        public void OpenDocument(string path, CancellationToken cancellationToken)
        {
            _messageService.SendRequest(
                OpenFileRequest.Type,
                new OpenFileDetails()
                {
                    FilePath = path,
                },
                waitForResponse: true);
        }

        public Task OpenDocumentAsync(string path)
        {
            return OpenDocumentAsync(path, CancellationToken.None);
        }

        public Task OpenDocumentAsync(string path, CancellationToken cancellationToken)
        {
            return _messageService.SendRequestAsync(
                OpenFileRequest.Type,
                new OpenFileDetails()
                {
                    FilePath = path,
                },
                waitForResponse: true);
        }

        public override void SetCursorPosition(int line, int column, CancellationToken cancellationToken)
        {
            _messageService.SendRequest(
                SetSelectionRequest.Type,
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
                },
                waitForResponse: true);
        }

        public override async Task SetCursorPositionAsync(int line, int column, CancellationToken cancellationToken)
        {
            await _messageService.SendRequestAsync(
                SetSelectionRequest.Type,
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
                },
                waitForResponse: true)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public override void SetSelection(
            int startLine,
            int startColumn,
            int endLine,
            int endColumn,
            CancellationToken cancellationToken)
        {
            _messageService.SendRequest(
                SetSelectionRequest.Type,
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
                },
                waitForResponse: true);
        }

        public override async Task SetSelectionAsync(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken)
        {
            await _messageService.SendRequestAsync(
                SetSelectionRequest.Type,
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
                },
                waitForResponse: true);
        }
    }
}
