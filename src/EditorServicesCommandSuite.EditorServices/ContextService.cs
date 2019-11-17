using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;
using Microsoft.PowerShell.EditorServices.Services.PowerShellContext;

using PSWorkspaceService = Microsoft.PowerShell.EditorServices.Services.WorkspaceService;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class ContextService : DocumentContextProvider
    {
        private readonly MessageService _messages;

        private readonly PSWorkspaceService _workspace;

        internal ContextService(PSWorkspaceService workspace, MessageService messages)
        {
            _workspace = workspace;
            _messages = messages;
        }

        internal override string Workspace => _workspace.WorkspacePath;

        internal override async Task<DocumentContextBase> GetDocumentContextAsync(
            PSCmdlet cmdlet,
            CancellationToken cancellationToken,
            ThreadController threadController)
        {
            var context = await _messages.SendRequestAsync(
                Messages.GetEditorContext,
                new GetEditorContextRequest())
                .ConfigureAwait(false);

            var scriptFile = _workspace.GetFile(context.CurrentFilePath);
            return GetContextBuilder(scriptFile.ScriptAst, scriptFile.ScriptTokens)
                .AddCursorPosition(
                    (int)context.CursorPosition.Line + 1,
                    (int)context.CursorPosition.Character + 1)
                .AddSelectionRange(
                    (int)context.SelectionRange.Start.Line + 1,
                    (int)context.SelectionRange.Start.Character,
                    (int)context.SelectionRange.End.Line + 1,
                    (int)context.SelectionRange.End.Character)
                .AddCancellationToken(cancellationToken)
                .AddCmdlet(cmdlet)
                .AddThreadController(threadController);
        }
    }
}
