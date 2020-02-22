using System;
using System.Threading;
using System.Threading.Tasks;

using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices.Extensions;
using Microsoft.PowerShell.EditorServices.Extensions.Services;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class EditorServicesNavigationService : NavigationService, INavigationSupportsOpenDocument
    {
        private readonly IEditorContextService _context;

        internal EditorServicesNavigationService(IEditorContextService context)
        {
            _context = context;
        }

        public void OpenDocument(string path)
        {
            OpenDocument(path, CancellationToken.None);
        }

        public void OpenDocument(string path, CancellationToken cancellationToken)
        {
            _context.OpenFileAsync(new Uri(path))
                .GetAwaiter()
                .GetResult();
        }

        public Task OpenDocumentAsync(string path)
        {
            return OpenDocumentAsync(path, CancellationToken.None);
        }

        public Task OpenDocumentAsync(string path, CancellationToken cancellationToken)
        {
            return _context.OpenFileAsync(new Uri(path));
        }

        public override void SetCursorPosition(int line, int column, CancellationToken cancellationToken)
        {
            _context.SetSelectionAsync(
                new LspRange(
                    new LspFilePosition(line - 1, column - 1),
                    new LspFilePosition(line - 1, column - 1)))
                .GetAwaiter()
                .GetResult();
        }

        public override async Task SetCursorPositionAsync(int line, int column, CancellationToken cancellationToken)
        {
            await _context.SetSelectionAsync(
                new LspRange(
                    new LspFilePosition(line - 1, column - 1),
                    new LspFilePosition(line - 1, column - 1)))
                .ConfigureAwait(false);
        }

        public override void SetSelection(
            int startLine,
            int startColumn,
            int endLine,
            int endColumn,
            CancellationToken cancellationToken)
        {
            _context.SetSelectionAsync(
                new LspRange(
                    new LspFilePosition(startLine - 1, startColumn - 1),
                    new LspFilePosition(endLine - 1, endColumn - 1)))
                .GetAwaiter()
                .GetResult();
        }

        public override async Task SetSelectionAsync(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken)
        {
            await _context.SetSelectionAsync(
                new LspRange(
                    new LspFilePosition(startLine - 1, startColumn - 1),
                    new LspFilePosition(endLine - 1, endColumn - 1)))
                .ConfigureAwait(false);
        }
    }
}
