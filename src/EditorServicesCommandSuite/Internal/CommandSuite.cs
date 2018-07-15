using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides a central entry point for interacting with the command suite session.
    /// </summary>
    public abstract class CommandSuite
    {
        private static CommandSuite s_instance;

        private InternalNavigationService _navigation;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSuite" /> class.
        /// </summary>
        /// <param name="engine">The PowerShell engine.</param>
        /// <param name="host">The PowerShell host.</param>
        protected CommandSuite(
            EngineIntrinsics engine,
            PSHost host)
        {
            if (s_instance != null)
            {
                throw new InvalidOperationException();
            }

            Refactors = new RefactorService();
            s_instance = this;
            ExecutionContext = engine;
        }

        internal InternalNavigationService InternalNavigation
        {
            get
            {
                if (_navigation != null)
                {
                    return _navigation;
                }

                return _navigation = new InternalNavigationService(GetNavigationServiceImpl());
            }
        }

        internal RefactorService Refactors { get; }

        internal PSHost Host { get; }

        /// <summary>
        /// Gets the current instance or <see langword="null" /> if it has not been
        /// created yet.
        /// </summary>
        protected internal static CommandSuite Instance
        {
            get
            {
                if (s_instance != null)
                {
                    return s_instance;
                }

                // Throw an exception with an error record to avoid null reference
                // exceptions if the editor host failed to properly initalize.
                throw new NoCommandSuiteInstanceException();
            }
        }

        /// <summary>
        /// Gets the diagnostics provider.
        /// </summary>
        protected internal abstract IRefactorAnalysisContext Diagnostics { get; }

        /// <summary>
        /// Gets the processor for <see cref="DocumentEdit" /> objects.
        /// </summary>
        protected internal abstract IDocumentEditProcessor Documents { get; }

        /// <summary>
        /// Gets the interface for interacting with the UI.
        /// </summary>
        protected internal abstract IRefactorUI UI { get; }

        /// <summary>
        /// Gets the interface for navigating an open document.
        /// </summary>
        protected internal NavigationService Navigation => InternalNavigation;

        /// <summary>
        /// Gets the interface for getting information about the users current
        /// state in an open document. (e.g. cursor position, selection, etc)
        /// </summary>
        protected internal abstract DocumentContextProvider DocumentContext { get; }

        /// <summary>
        /// Gets the interface for safely invoking PowerShell commands.
        /// </summary>
        protected internal abstract IPowerShellExecutor Execution { get; }

        /// <summary>
        /// Gets the interface for getting information about the state of the
        /// current workspace.
        /// </summary>
        protected internal virtual IRefactorWorkspace Workspace { get; } = new WorkspaceContext();

        /// <summary>
        /// Gets the interface for the PowerShell engine.
        /// </summary>
        protected internal EngineIntrinsics ExecutionContext { get; }

        /// <summary>
        /// Generates the cdxml for cmdletizing refactor providers and writes it to a file.
        /// </summary>
        /// <param name="path">The path to save the cdxml to.</param>
        public static void WriteRefactorModule(string path)
        {
            Cmdletizer.WriteRefactorModule(path);
        }

        /// <summary>
        /// Registers PowerShell based refactor providers to the refactor list.
        /// </summary>
        /// <param name="session">
        /// The <see cref="SessionState" /> where the refactor functions are loaded.
        /// </param>
        public void ImportSessionRefactors(SessionState session)
        {
            var functions = session.InvokeCommand.GetCommands(
                "*",
                CommandTypes.Function,
                nameIsPattern: true)
                .Where(function =>
                {
                    return function.ModuleName.Equals(session.Module.Name, StringComparison.Ordinal)
                        && ((FunctionInfo)function).ScriptBlock.Attributes
                            .Any(a => a is ScriptBasedRefactorProviderAttribute);
                });

            foreach (var function in functions)
            {
                Refactors.RegisterProvider(
                    new PowerShellRefactorProvider(
                        Execution,
                        (FunctionInfo)function));
            }
        }

        /// <summary>
        /// Requests refactor options based on the current state of the host editor.
        /// </summary>
        /// <param name="cmdlet">The <see cref="PSCmdlet" /> to use for context.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        public async Task RequestRefactor(PSCmdlet cmdlet)
        {
            Validate.IsNotNull(nameof(cmdlet), cmdlet);
            await RequestRefactor(
                cmdlet,
                TaskCmdletAdapter.GetStoppingCancellationToken(cmdlet));
        }

        /// <summary>
        /// Requests refactor options based on the current state of the host editor.
        /// </summary>
        /// <param name="cmdlet">The <see cref="PSCmdlet" /> to use for context.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        public async Task RequestRefactor(PSCmdlet cmdlet, CancellationToken cancellationToken)
        {
            Validate.IsNotNull(nameof(cmdlet), cmdlet);
            await RequestRefactor(
                cmdlet,
                await DocumentContext.GetDocumentContextAsync(cmdlet, cancellationToken));
        }

        /// <summary>
        /// Requests refactor options based on the current state of the host editor.
        /// </summary>
        /// <param name="cmdlet">The <see cref="PSCmdlet" /> to use for context.</param>
        /// <param name="request">The context of the refactor request.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        internal async Task RequestRefactor(PSCmdlet cmdlet, DocumentContextBase request)
        {
            Validate.IsNotNull(nameof(cmdlet), cmdlet);
            Validate.IsNotNull(nameof(request), request);
            var refactors = new List<IRefactorInfo>();
            foreach (var refactor in Refactors.GetRefactorOptions(request))
            {
                cmdlet.ThrowIfStopping();
                refactors.Add(refactor);
            }

            if (!refactors.Any())
            {
                return;
            }

            var selectedRefactor = await UI.ShowChoicePromptAsync(
                RefactorStrings.SelectRefactorCaption,
                RefactorStrings.SelectRefactorMessage,
                refactors.ToArray(),
                item => item.Name,
                item => item.Description);

            await Documents.WriteDocumentEditsAsync(await selectedRefactor.GetDocumentEdits());
            if (request.SelectionRange != null)
            {
                await Navigation.SetSelectionAsync(
                    request.SelectionRange.Item1,
                    request.SelectionRange.Item2,
                    request.SelectionRange.Item3,
                    request.SelectionRange.Item4,
                    request.CancellationToken);
            }
        }

        /// <summary>
        /// Get the <see cref="NavigationService" /> that will be saved
        /// to <see cref="CommandSuite.Navigation" />.
        /// </summary>
        /// <returns>The <see cref="NavigationService" />.</returns>
        protected abstract NavigationService GetNavigationServiceImpl();

        /// <summary>
        /// Creates and registers default refactor providers.
        /// </summary>
        protected void InitializeRefactorProviders()
        {
            Refactors.RegisterProvider(new ImplementAbstractMethodsRefactor(UI));
            Refactors.RegisterProvider(new DropNamespaceRefactor());
            Refactors.RegisterProvider(new CommandSplatRefactor(UI));
            Refactors.RegisterProvider(new ChangeStringEnclosureRefactor(UI));
            Refactors.RegisterProvider(new SurroundSelectedLinesRefactor(UI, Navigation));
            Refactors.RegisterProvider(new SuppressAnalyzerMessageRefactor(Diagnostics));
            Refactors.RegisterProvider(new AddModuleQualificationRefactor(Execution, UI, Workspace));
        }
    }
}
