using System.Management.Automation;
using System.Management.Automation.Host;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.PSReadLine.Internal
{
    public class CommandSuite : EditorServicesCommandSuite.Internal.CommandSuite
    {
        private readonly PSReadLineNavigationService _navigation;

        protected CommandSuite(EngineIntrinsics engine, PSHost host)
            : base(engine, host)
        {
            Documents = new DocumentService();
            UI = new UIService(host);
            DocumentContext = new ContextService();
            Diagnostics = new NullDiagnosticService();
            _navigation = new PSReadLineNavigationService();
            Execution = new ExecutionService();
        }

        protected override IRefactorAnalysisContext Diagnostics { get; }

        protected override IDocumentEditProcessor Documents { get; }

        protected override IRefactorUI UI { get; }

        protected override DocumentContextProvider DocumentContext { get; }

        protected override IPowerShellExecutor Execution { get; }

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

        protected override NavigationService GetNavigationServiceImpl()
        {
            return _navigation;
        }
    }
}
