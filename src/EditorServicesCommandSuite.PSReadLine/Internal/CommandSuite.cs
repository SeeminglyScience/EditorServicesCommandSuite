using System.Management.Automation;
using System.Management.Automation.Host;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.PSReadLine.Internal
{
    /// <summary>
    /// Provides a central entry point for interacting with a PSReadLine based command
    /// suite session.
    /// </summary>
    public class CommandSuite : EditorServicesCommandSuite.Internal.CommandSuite
    {
        private readonly PSReadLineNavigationService _navigation;

        private CommandSuite(EngineIntrinsics engine, PSHost host)
            : base(engine, host)
        {
            Documents = new DocumentService();
            UI = new UIService(host);
            DocumentContext = new ContextService();
            Diagnostics = new NullDiagnosticService();
            _navigation = new PSReadLineNavigationService();
            Execution = new ExecutionService();
        }

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
        /// Gets the command suite instance for the process, or creates
        /// it if it does not exist yet.
        /// </summary>
        /// <param name="engine">The PowerShell engine.</param>
        /// <param name="host">The PowerShell host.</param>
        /// <returns>
        /// The command suite instance for the process.
        /// </returns>
        public static EditorServicesCommandSuite.Internal.CommandSuite GetCommandSuite(
            EngineIntrinsics engine,
            PSHost host)
        {
            if (Instance != null)
            {
                return Instance;
            }

            var commandSuite = new CommandSuite(engine, host);
            commandSuite.InitializeRefactorProviders();
            return commandSuite;
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
