using System.Management.Automation;
using System.Management.Automation.Host;
using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices;
using Microsoft.PowerShell.EditorServices.Extensions;
using Microsoft.PowerShell.EditorServices.Utility;

namespace EditorServicesCommandSuite.EditorServices.Internal
{
    public class CommandSuite : EditorServicesCommandSuite.Internal.CommandSuite
    {
        private const string EditorOperationsFieldName = "editorOperations";

        private const string EditorSessionFieldName = "editorSession";

        private static CommandSuite s_instance;

        private readonly EditorServicesNavigationService _navigation;

        private CommandSuite(
            EditorObject psEditor,
            EngineIntrinsics engine,
            PSHost host,
            PowerShellContext internalContext)
            : base(engine, host)
        {
            Editor = psEditor;
            Messages = new MessageService(Editor);
            UI = new UIService(Messages);
            _navigation = new EditorServicesNavigationService(Messages);
            Execution = new ExecutionService(psEditor, this, internalContext, ExecutionContext);
            Diagnostics = new DiagnosticsService(Execution);

            var editorOperations =
                typeof(EditorObject)
                    .GetField(
                        EditorOperationsFieldName,
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(Editor);

            EditorSession =
                (EditorSession)editorOperations
                    .GetType()
                    .GetField(
                        EditorSessionFieldName,
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .GetValue(editorOperations);

            Documents = new DocumentService(EditorSession, Messages);
            DocumentContext = new ContextService(EditorSession.Workspace, Messages);
            Workspace = new WorkspaceService(engine, EditorSession.Workspace);
        }

        internal static new CommandSuite Instance => s_instance;

        internal MessageService Messages { get; }

        internal EditorObject Editor { get; }

        internal EditorSession EditorSession { get; }

        protected override IRefactorAnalysisContext Diagnostics { get; }

        protected override IDocumentEditProcessor Documents { get; }

        protected override IRefactorUI UI { get; }

        protected override DocumentContextProvider DocumentContext { get; }

        protected override IPowerShellExecutor Execution { get; }

        protected override IRefactorWorkspace Workspace { get; }

        public static CommandSuite GetCommandSuite(
            EditorObject psEditor,
            EngineIntrinsics engine,
            PSHost host,
            PowerShellContext internalContext)
        {
            Validate.IsNotNull(nameof(engine), engine);
            Validate.IsNotNull(nameof(psEditor), psEditor);
            Validate.IsNotNull(nameof(host), host);

            if (Instance != null)
            {
                return Instance;
            }

            s_instance = new CommandSuite(psEditor, engine, host, internalContext);
            s_instance.InitializeRefactorProviders();
            return s_instance;
        }

        protected override NavigationService GetNavigationServiceImpl()
        {
            return _navigation;
        }
    }
}
