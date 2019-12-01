using System;
using System.ComponentModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides a central entry point for interacting with the command suite session.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class CommandSuite
    {
        private static CommandSuite s_instance;

        private InternalNavigationService _navigation;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandSuite" /> class.
        /// </summary>
        /// <param name="engine">The PowerShell engine.</param>
        /// <param name="host">The PowerShell host.</param>
        private protected CommandSuite(
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

        /// <summary>
        /// Gets the current instance or <see langword="null" /> if it has not been
        /// created yet.
        /// </summary>
        internal static CommandSuite Instance
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

        internal InternalNavigationService InternalNavigation
            => _navigation ?? (_navigation = new InternalNavigationService(GetNavigationServiceImpl()));

        internal RefactorService Refactors { get; }

        internal PSHost Host { get; }

        /// <summary>
        /// Gets the diagnostics provider.
        /// </summary>
        internal abstract IRefactorAnalysisContext Diagnostics { get; }

        /// <summary>
        /// Gets the processor for <see cref="DocumentEdit" /> objects.
        /// </summary>
        internal abstract IDocumentEditProcessor Documents { get; }

        /// <summary>
        /// Gets the interface for interacting with the UI.
        /// </summary>
        internal abstract IRefactorUI UI { get; }

        /// <summary>
        /// Gets the interface for navigating an open document.
        /// </summary>
        internal NavigationService Navigation => InternalNavigation;

        /// <summary>
        /// Gets the interface for getting information about the users current
        /// state in an open document (e.g. cursor position, selection, etc).
        /// </summary>
        internal abstract DocumentContextProvider DocumentContext { get; }

        /// <summary>
        /// Gets the interface for getting information about the state of the
        /// current workspace.
        /// </summary>
        internal virtual IRefactorWorkspace Workspace { get; } = new WorkspaceContext();

        /// <summary>
        /// Gets the interface for the PowerShell engine.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal EngineIntrinsics ExecutionContext { get; }

        /// <summary>
        /// Generates the cdxml for cmdletizing refactor providers and writes it to a file.
        /// </summary>
        /// <param name="path">The path to save the cdxml to.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void WriteRefactorModule(string path)
        {
            Cmdletizer.WriteRefactorModule(path);
        }

        /// <summary>
        /// Requests refactor options based on the current state of the host editor.
        /// </summary>
        /// <param name="cmdlet">The <see cref="PSCmdlet" /> to use for context.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public virtual void RequestRefactor(PSCmdlet cmdlet)
        {
            var threadController = new ThreadController(ExecutionContext, cmdlet);
            var cancellationToken = TaskCmdletAdapter.GetStoppingCancellationToken(cmdlet);
            Task refactorRequest = Task.Run(
                async () => await RequestRefactor(
                    cmdlet,
                    await DocumentContext.GetDocumentContextAsync(
                        cmdlet,
                        cancellationToken,
                        threadController)
                        .ConfigureAwait(false))
                    .ConfigureAwait(false));

            threadController.GiveControl(refactorRequest, cancellationToken);
        }

        internal static bool TryGetInstance(out CommandSuite instance)
        {
            instance = s_instance;
            return s_instance != null;
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
            CodeAction[] actions = await Refactors.GetCodeActionsAsync(request).ConfigureAwait(false);
            if (actions == null || actions.Length == 0)
            {
                return;
            }

            CodeAction selectedAction = await UI.ShowChoicePromptAsync(
                RefactorStrings.SelectRefactorCaption,
                RefactorStrings.SelectRefactorMessage,
                actions,
                item => item.Title)
                .ConfigureAwait(false);

            await selectedAction.ComputeChanges(request).ConfigureAwait(false);
            WorkspaceChange[] changes = await request.FinalizeWorkspaceChanges().ConfigureAwait(false);

            await ProcessWorkspaceChanges(changes, request.CancellationToken).ConfigureAwait(false);

            if (request.SelectionRange != null)
            {
                await Navigation.SetSelectionAsync(
                    request.SelectionRange.Item1,
                    request.SelectionRange.Item2,
                    request.SelectionRange.Item3,
                    request.SelectionRange.Item4,
                    request.CancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates and registers default refactor providers.
        /// </summary>
        internal void InitializeRefactorProviders()
        {
            Refactors.RegisterProvider(new ImplementAbstractMethodsRefactor(UI));
            Refactors.RegisterProvider(new DropNamespaceRefactor());
            Refactors.RegisterProvider(new CommandSplatRefactor(UI));
            Refactors.RegisterProvider(new ChangeStringEnclosureRefactor());
            Refactors.RegisterProvider(new SurroundSelectedLinesRefactor(UI));
            Refactors.RegisterProvider(new SuppressAnalyzerMessageRefactor(Diagnostics));
            Refactors.RegisterProvider(new AddModuleQualificationRefactor(UI, Workspace));
            Refactors.RegisterProvider(new ExpandMemberExpressionRefactor(UI));
            Refactors.RegisterProvider(new ExtractFunctionRefactor(UI, Workspace));
            Refactors.RegisterProvider(new NameUnnamedBlockRefactor());
            Refactors.RegisterProvider(new RegisterCommandExportRefactor(Workspace));
        }

        internal async Task ProcessWorkspaceChanges(WorkspaceChange[] changes, CancellationToken cancellationToken = default)
        {
            foreach (WorkspaceChange change in changes)
            {
                switch (change.Type)
                {
                    case WorkspaceChangeType.Edit:
                    {
                        await Documents.WriteDocumentEditsAsync(
                            change.Edits,
                            cancellationToken)
                            .ConfigureAwait(false);
                        break;
                    }

                    case WorkspaceChangeType.Delete: break;
                    case WorkspaceChangeType.Move: break;
                    case WorkspaceChangeType.New: break;
                    case WorkspaceChangeType.Rename: break;
                    default: throw new ArgumentOutOfRangeException(nameof(change.Type));
                }
            }
        }

        /// <summary>
        /// Get the <see cref="NavigationService" /> that will be saved
        /// to <see cref="CommandSuite.Navigation" />.
        /// </summary>
        /// <returns>The <see cref="NavigationService" />.</returns>
        private protected abstract NavigationService GetNavigationServiceImpl();
    }
}
