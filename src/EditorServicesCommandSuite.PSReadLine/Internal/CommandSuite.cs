using System.Management.Automation;
using System.Management.Automation.Host;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.PSReadLine.Internal
{
    /// <summary>
    /// Provides a central entry point for interacting with a PSReadLine based command
    /// suite session.
    /// </summary>
    public sealed class CommandSuite : EditorServicesCommandSuite.Internal.CommandSuite
    {
        private readonly PSReadLineNavigationService _navigation;

        private CommandSuite(EngineIntrinsics engine, PSHost host)
            : base(engine, host)
        {
            Documents = new DocumentService();
            UI = new UIService();
            DocumentContext = new ContextService();
            Diagnostics = new NullDiagnosticService();
            _navigation = new PSReadLineNavigationService();
        }

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
            try
            {
                return Instance;
            }
            catch (NoCommandSuiteInstanceException)
            {
                // Exception is thrown when CommandSuite has not yet been created.
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
        private protected override NavigationService GetNavigationServiceImpl()
        {
            return _navigation;
        }
    }
}
