using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices;
using Microsoft.PowerShell.EditorServices.Protocol.LanguageServer;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class ContextService : DocumentContextProvider
    {
        private readonly MessageService _messages;
        private readonly Workspace _workspace;

        internal ContextService(Workspace workspace, MessageService messages)
        {
            _workspace = workspace;
            _messages = messages;
        }

        protected override string Workspace => _workspace.WorkspacePath;

        protected override Task<DocumentContextBase> GetDocumentContextAsync()
        {
            return GetDocumentContextAsync(null, CancellationToken.None);
        }

        protected override Task<DocumentContextBase> GetDocumentContextAsync(CancellationToken cancellationToken)
        {
            return GetDocumentContextAsync(null, cancellationToken);
        }

        protected override Task<DocumentContextBase> GetDocumentContextAsync(PSCmdlet cmdlet)
        {
            return GetDocumentContextAsync(cmdlet, CancellationToken.None);
        }

        protected override async Task<DocumentContextBase> GetDocumentContextAsync(
            PSCmdlet cmdlet,
            CancellationToken cancellationToken)
        {
            var context = await _messages.Sender.SendRequest(
                GetEditorContextRequest.Type,
                new GetEditorContextRequest(),
                waitForResponse: true);

            var scriptFile = _workspace.GetFile(context.CurrentFilePath);
            return GetContextBuilder(scriptFile.ScriptAst, scriptFile.ScriptTokens)
                .AddCursorPosition(
                    context.CursorPosition.Line + 1,
                    context.CursorPosition.Character + 1)
                .AddSelectionRange(
                    context.SelectionRange.Start.Line + 1,
                    context.SelectionRange.Start.Character,
                    context.SelectionRange.End.Line + 1,
                    context.SelectionRange.End.Character)
                .AddCancellationToken(cancellationToken)
                .AddCmdlet(cmdlet);
        }
    }
}
