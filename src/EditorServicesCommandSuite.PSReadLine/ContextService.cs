using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal class ContextService : DocumentContextProvider
    {
        protected override string Workspace => string.Empty;

        protected override Task<DocumentContextBase> GetDocumentContextAsync()
        {
            return GetDocumentContextAsync(null, CancellationToken.None);
        }

        protected override Task<DocumentContextBase> GetDocumentContextAsync(PSCmdlet cmdlet)
        {
            return GetDocumentContextAsync(cmdlet, CancellationToken.None);
        }

        protected override Task<DocumentContextBase> GetDocumentContextAsync(CancellationToken cancellationToken)
        {
            return GetDocumentContextAsync(null, cancellationToken);
        }

        protected override Task<DocumentContextBase> GetDocumentContextAsync(
            PSCmdlet cmdlet,
            CancellationToken cancellationToken)
        {
            PSConsoleReadLine.GetSelectionState(
                out int selectionStart,
                out int selectionLength);
            PSConsoleReadLine.GetBufferState(
                out Ast ast,
                out Token[] tokens,
                out _,
                out int cursor);

            return Task.FromResult(
                (DocumentContextBase)GetContextBuilder(ast, tokens)
                    .AddCursorPosition(cursor)
                    .AddCancellationToken(cancellationToken)
                    .AddSelectionRange(selectionStart, selectionStart + selectionLength)
                    .AddCmdlet(cmdlet));
        }
    }
}
