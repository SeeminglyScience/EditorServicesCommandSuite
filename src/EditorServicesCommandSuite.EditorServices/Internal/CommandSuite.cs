using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Host;

using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;
using Microsoft.PowerShell.EditorServices.Extensions;
using Microsoft.PowerShell.EditorServices.Extensions.Services;

namespace EditorServicesCommandSuite.EditorServices.Internal
{
    /// <summary>
    /// Provides a central entry point for interacting with a Editor Services based command
    /// suite session.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never), DebuggerStepThrough]
    public sealed class CommandSuite : EditorServicesCommandSuite.Internal.CommandSuite
    {
        private static CommandSuite s_instance;

        private readonly EditorServicesNavigationService _navigation;

        private readonly EditorExtensionServiceProvider _extensionServiceProvider;

        private CommandSuite(
            EditorObject psEditor,
            EngineIntrinsics engine,
            PSHost host)
            : base(engine, host)
        {
            Editor = psEditor;
            _extensionServiceProvider = psEditor.GetExtensionServiceProvider();
            IWorkspaceService workspace = _extensionServiceProvider.Workspace;
            IEditorContextService context = _extensionServiceProvider.EditorContext;
            ILanguageServerService messages = _extensionServiceProvider.LanguageServer;
            IEditorUIService ui = _extensionServiceProvider.EditorUI;

            UI = new UIService(messages, ui);
            _navigation = new EditorServicesNavigationService(context);
            Diagnostics = new DiagnosticsService();
            Documents = new DocumentService(workspace, context, messages);
            DocumentContext = new ContextService(workspace, context);
            Workspace = new WorkspaceService(engine, workspace, messages);
        }

        internal static new CommandSuite Instance => s_instance;

        internal EditorObject Editor { get; }

        /// <summary>
        /// Gets the diagnostics provider.
        /// </summary>
        internal override IRefactorAnalysisContext Diagnostics { get; }

        /// <summary>
        /// Gets the processor for <see cref="DocumentEdit" /> objects.
        /// </summary>
        internal override IDocumentEditProcessor Documents { get; }

        /// <summary>
        /// Gets the interface for interacting with the UI.
        /// </summary>
        internal override IRefactorUI UI { get; }

        /// <summary>
        /// Gets the interface for getting information about the users current
        /// state in an open document (e.g. cursor position, selection, etc).
        /// </summary>
        internal override DocumentContextProvider DocumentContext { get; }

        /// <summary>
        /// Gets the interface for getting information about the state of the
        /// current workspace.
        /// </summary>
        internal override IRefactorWorkspace Workspace { get; }

        /// <summary>
        /// Gets the command suite instance for the process, or creates
        /// it if it does not exist yet.
        /// </summary>
        /// <param name="psEditor">The psEditor variable from the integrated terminal.</param>
        /// <param name="engine">The PowerShell engine.</param>
        /// <param name="host">The PowerShell host.</param>
        /// <returns>
        /// The command suite instance for the process.
        /// </returns>
        [Hidden, Obsolete(StringLiterals.InternalUseOnly, error: true), EditorBrowsable(EditorBrowsableState.Never)]
        public static CommandSuite GetCommandSuite(
            EditorObject psEditor,
            EngineIntrinsics engine,
            PSHost host)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (psEditor == null)
            {
                throw new ArgumentNullException(nameof(psEditor));
            }

            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (s_instance != null)
            {
                return s_instance;
            }

            s_instance = new CommandSuite(psEditor, engine, host);
            s_instance.InitializeRefactorProviders();
            return s_instance;
        }

        /// <summary>
        /// Get the <see cref="NavigationService" /> that will be used to create the internal
        /// navigation service.
        /// </summary>
        /// <returns>The <see cref="NavigationService" />.</returns>
        private protected override NavigationService GetNavigationServiceImpl()
        {
            return _navigation;
        }
    }
}
