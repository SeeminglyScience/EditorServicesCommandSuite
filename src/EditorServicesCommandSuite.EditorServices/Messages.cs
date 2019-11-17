using Microsoft.PowerShell.EditorServices.Handlers;
using Microsoft.PowerShell.EditorServices.Services.PowerShellContext;

namespace EditorServicesCommandSuite.EditorServices
{
    internal static class Messages
    {
        public static FuncMessage<ShowInputPromptRequest, ShowInputPromptResponse> ShowInputPrompt
            = new FuncMessage<ShowInputPromptRequest, ShowInputPromptResponse>("powerShell/showInputPrompt");

        public static FuncMessage<ShowChoicePromptRequest, ShowChoicePromptResponse> ShowChoicePrompt
            = new FuncMessage<ShowChoicePromptRequest, ShowChoicePromptResponse>("powerShell/showChoicePrompt");

        public static ActionMessage<string> ShowWarningMessage = new ActionMessage<string>("editor/showWarningMessage");

        public static ActionMessage<string> ShowErrorMessage = new ActionMessage<string>("editor/showErrorMessage");

        public static ActionMessage<StatusBarMessageDetails> SetStatusBarMessage = new ActionMessage<StatusBarMessageDetails>("editor/setStatusBarMessage");

        public static ActionMessage<OpenFileDetails> OpenFile = new ActionMessage<OpenFileDetails>("editor/openFile");

        public static ActionMessage<SetSelectionRequest> SetSelection = new ActionMessage<SetSelectionRequest>("editor/setSelection");

        public static ActionMessage<InsertTextRequest> InsertText = new ActionMessage<InsertTextRequest>("editor/insertText");

        public static ActionMessage<string> NewFile = new ActionMessage<string>("editor/newFile");

        public static ActionMessage<SaveFileDetails> SaveFile = new ActionMessage<SaveFileDetails>("editor/saveFile");

        public static FuncMessage<GetEditorContextRequest, ClientEditorContext> GetEditorContext = new FuncMessage<GetEditorContextRequest, ClientEditorContext>("editor/getEditorContext");
    }
}
