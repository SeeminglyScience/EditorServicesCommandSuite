using System;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class InternalNavigationService : NavigationService, INavigationSupportsOpenDocument
    {
        private NavigationService _external;

        private INavigationSupportsOpenDocument _documentOpener;

        internal InternalNavigationService(NavigationService externalService)
        {
            ExternalService = externalService;
        }

        internal NavigationService ExternalService
        {
            get
            {
                return _external;
            }

            set
            {
                _external = value;
                _documentOpener = value as INavigationSupportsOpenDocument;
            }
        }

        internal bool DoesSupportOpenDocument => _documentOpener != null;

        public void OpenDocument(string path)
        {
            if (DoesSupportOpenDocument)
            {
                _documentOpener.OpenDocument(path);
            }

            throw new NotSupportedException();
        }

        public void OpenDocument(string path, CancellationToken cancellationToken)
        {
            if (DoesSupportOpenDocument)
            {
                _documentOpener.OpenDocument(path, cancellationToken);
            }

            throw new NotSupportedException();
        }

        public Task OpenDocumentAsync(string path)
        {
            if (DoesSupportOpenDocument)
            {
                return _documentOpener.OpenDocumentAsync(path);
            }

            throw new NotSupportedException();
        }

        public Task OpenDocumentAsync(string path, CancellationToken cancellationToken)
        {
            if (DoesSupportOpenDocument)
            {
                return _documentOpener.OpenDocumentAsync(path, cancellationToken);
            }

            throw new NotSupportedException();
        }

        public override void SetCursorPosition(int line, int column, CancellationToken cancellationToken)
        {
            ExternalService.SetCursorPosition(line, column, cancellationToken);
        }

        public override Task SetCursorPositionAsync(int line, int column, CancellationToken cancellationToken)
        {
            return ExternalService.SetCursorPositionAsync(line, column, cancellationToken);
        }

        public override void SetSelection(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken)
        {
            ExternalService.SetSelection(
                startLine,
                startColumn,
                endLine,
                endColumn,
                cancellationToken);
        }

        public override Task SetSelectionAsync(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken)
        {
            return ExternalService.SetSelectionAsync(
                startLine,
                startColumn,
                endLine,
                endColumn,
                cancellationToken);
        }
    }
}
