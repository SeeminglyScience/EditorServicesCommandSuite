using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;
using Microsoft.PowerShell.EditorServices.Extensions.Services;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class ContextService : DocumentContextProvider
    {
        private readonly IWorkspaceService _workspace;

        private readonly IEditorContextService _context;

        internal ContextService(IWorkspaceService workspace, IEditorContextService context)
        {
            _workspace = workspace;
            _context = context;
        }

        internal override string Workspace => _workspace.WorkspacePath;

        internal override async Task<DocumentContextBase> GetDocumentContextAsync(
            PSCmdlet cmdlet,
            CancellationToken cancellationToken,
            ThreadController threadController)
        {
            var context = await _context.GetCurrentLspFileContextAsync().ConfigureAwait(false);
            var scriptFile = _workspace.GetFile(context.Uri);
            return GetContextBuilder(scriptFile.Ast, scriptFile.Tokens.ToArray())
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
