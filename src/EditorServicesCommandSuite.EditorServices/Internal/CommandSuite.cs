using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Host;
using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices;
using Microsoft.PowerShell.EditorServices.Extensions;
using Microsoft.PowerShell.EditorServices.Utility;

namespace EditorServicesCommandSuite.EditorServices.Internal
{
    /// <summary>
    /// Provides a central entry point for interacting with a Editor Services based command
    /// suite session.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never), DebuggerStepThrough]
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

        /// <summary>
        /// Gets the diagnostics provider.
        /// </summary>
        protected override IRefactorAnalysisContext Diagnostics { get; }

        /// <summary>
        /// Gets the processor for <see cref="DocumentEdit" /> objects.
        /// </summary>
        protected override IDocumentEditProcessor Documents { get; }

        /// <summary>
        /// Gets the interface for interacting with the UI.
        /// </summary>
        protected override IRefactorUI UI { get; }

        /// <summary>
        /// Gets the interface for getting information about the users current
        /// state in an open document. (e.g. cursor position, selection, etc)
        /// </summary>
        protected override DocumentContextProvider DocumentContext { get; }

        /// <summary>
        /// Gets the interface for safely invoking PowerShell commands.
        /// </summary>
        protected override IPowerShellExecutor Execution { get; }

        /// <summary>
        /// Gets the interface for getting information about the state of the
        /// current workspace.
        /// </summary>
        protected override IRefactorWorkspace Workspace { get; }

        /// <summary>
        /// Gets the command suite instance for the process, or creates
        /// it if it does not exist yet.
        /// </summary>
        /// <param name="psEditor">The psEditor variable from the integrated terminal.</param>
        /// <param name="engine">The PowerShell engine.</param>
        /// <param name="host">The PowerShell host.</param>
        /// <param name="internalContext">The PowerShellContext to use for invoking commands.</param>
        /// <returns>
        /// The command suite instance for the process.
        /// </returns>
        [Obsolete("do not use this method", error: true), EditorBrowsable(EditorBrowsableState.Never)]
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

        /// <summary>
        /// Get the <see cref="NavigationService" /> that will be used to create the internal
        /// navigation service.
        /// </summary>
        /// <returns>The <see cref="NavigationService" />.</returns>
        protected override NavigationService GetNavigationServiceImpl()
        {
            return _navigation;
        }
    }
}
