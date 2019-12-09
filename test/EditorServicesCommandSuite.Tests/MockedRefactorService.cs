using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Tests
{
    internal class MockedRefactorService
    {
        private readonly RefactorProvider _provider;

        public MockedRefactorService(RefactorProvider provider)
        {
            _provider = provider;
        }

        public async Task<string> GetRefactoredString(
            string script,
            Func<DocumentContextBase, Task> factory,
            CancellationToken cancellationToken = default,
            RefactorConfiguration configuration = null,
            bool requiresRunspace = false)
        {
            using var mock = await CreateContextAsync(
                script,
                cancellationToken,
                configuration,
                requiresRunspace)
                .ConfigureAwait(false);

            WorkspaceChange[] changes = await mock.GetWorkspaceChangesAsync(factory).ConfigureAwait(false);
            return mock.ProcessChanges(changes);
        }

        public async Task<TemporaryContext> CreateContextAsync(
            string script,
            CancellationToken cancellationToken = default,
            RefactorConfiguration configuration = null,
            bool requiresRunspace = false)
        {
            Settings.SetSetting("NewLine", "\n");
            Settings.SetSetting("TabString", "\t");
            var (ast, tokens, cursorPosition, selection) = ParseScript(script);
            ThreadController controller = null;
            PSCmdlet cmdlet = null;
            RunspaceHandle handle = null;
            if (requiresRunspace)
            {
                handle = await RunspaceHandle.CreateAsync(cancellationToken).ConfigureAwait(false);
                (_, _, controller, cmdlet) = handle;
            }

            DocumentContextBase context = new DocumentContext(
                (ScriptBlockAst)ast,
                ast.FindAstAt(cursorPosition),
                new TokenCollection(tokens.AsMemory()).First
                    .FindNextOrSelf().ClosestTo(cursorPosition)
                    .GetResult(),
                selection,
                cmdlet,
                cancellationToken,
                controller);

            if (configuration != null)
            {
                context = new ConfiguredDocumentContext<RefactorConfiguration>(
                    configuration,
                    context);
            }

            return new TemporaryContext(this, context, handle);
        }

        private static ParsedScript ParseScript(string script)
        {
            var sb = new StringBuilder(script);
            var cursor = script.IndexOf("{{c}}");
            if (cursor >= 0)
            {
                sb.Remove(cursor, 5);
            }
            else
            {
                cursor = script.Length - 1;
            }

            int selectionStart = script.IndexOf("{{ss}}");
            int selectionEnd;
            if (selectionStart >= 0)
            {
                sb.Remove(selectionStart, 6);
                selectionEnd = script.IndexOf("{{se}}") - 6;
                sb.Remove(selectionEnd, 6);
                cursor = selectionEnd;
            }
            else
            {
                selectionStart = cursor;
                selectionEnd = cursor;
            }

            var (ast, tokens, position) = CommandCompletion.MapStringInputToParsedInput(sb.ToString(), cursor);
            return new ParsedScript(
                ast,
                tokens,
                position,
                PositionUtilities.NewScriptExtent(
                    ast.Extent,
                    selectionStart,
                    selectionEnd));
        }

        internal class TemporaryContext : IDisposable
        {
            private readonly MockedRefactorService _parent;

            private bool _isDisposed;

            private IDisposable _runspaceHandle;

            public TemporaryContext(MockedRefactorService parent, DocumentContextBase context, IDisposable handle)
            {
                _parent = parent;
                _runspaceHandle = handle;
                Context = context;
            }

            public DocumentContextBase Context { get; }

            public async Task<CodeAction[]> GetCodeActionsAsync()
            {
                await _parent._provider.ComputeCodeActions(Context).ConfigureAwait(false);
                return await Context.FinalizeCodeActions().ConfigureAwait(false);
            }

            public async Task<WorkspaceChange[]> GetWorkspaceChangesAsync(
                Func<DocumentContextBase, Task> computeChanges)
            {
                await computeChanges(Context).ConfigureAwait(false);
                return await Context.FinalizeWorkspaceChanges().ConfigureAwait(false);
            }

            public async Task<WorkspaceChange[]> GetWorkspaceChangesAsync<T>(
                Func<DocumentContextBase, T, Task> computeChanges,
                T state)
            {
                await computeChanges(Context, state).ConfigureAwait(false);
                return await Context.FinalizeWorkspaceChanges().ConfigureAwait(false);
            }

            public async Task<WorkspaceChange[]> GetWorkspaceChangesAsync<T, T1>(
                Func<DocumentContextBase, T, T1, Task> computeChanges,
                (T arg0, T1 arg1) state)
            {
                await computeChanges(Context, state.arg0, state.arg1).ConfigureAwait(false);
                return await Context.FinalizeWorkspaceChanges().ConfigureAwait(false);
            }

            public async Task<WorkspaceChange[]> GetWorkspaceChangesAsync<T, T1, T2>(
                Func<DocumentContextBase, T, T1, T2, Task> computeChanges,
                (T arg0, T1 arg1, T2 arg2) state)
            {
                await computeChanges(Context, state.arg0, state.arg1, state.arg2).ConfigureAwait(false);
                return await Context.FinalizeWorkspaceChanges().ConfigureAwait(false);
            }

            public void Dispose() => Dispose(true);

            protected virtual void Dispose(bool disposing)
            {
                if (_isDisposed)
                {
                    return;
                }

                if (disposing)
                {
                    var handle = _runspaceHandle;
                    _runspaceHandle = null;
                    handle?.Dispose();
                }

                _isDisposed = true;
            }

            public string ProcessChanges(WorkspaceChange[] changes)
            {
                var sb = new StringBuilder(Context.RootAst.Extent.Text);
                foreach (WorkspaceChange change in changes)
                {
                    if (change.Type != WorkspaceChangeType.Edit)
                    {
                        continue;
                    }

                    foreach (DocumentEdit edit in change.Edits.OrderByDescending(e => e.StartOffset))
                    {
                        if (edit == null)
                        {
                            continue;
                        }

                        if (!string.IsNullOrEmpty(edit.OriginalValue))
                        {
                            sb.Remove((int)edit.StartOffset, edit.OriginalValue.Length);
                        }

                        sb.Insert((int)edit.StartOffset, edit.NewValue);
                    }
                }

                return sb.ToString();
            }
        }

        private class RunspaceHandle : IDisposable
        {
            private bool _isDisposed;

            private RunspaceHandle(
                PowerShell powerShell,
                Runspace runspace,
                ThreadController pipelineThread,
                PSCmdlet cmdlet,
                CancellationTokenSource cancellation)
            {
                PowerShell = powerShell;
                Runspace = runspace;
                PipelineThread = pipelineThread;
                Cmdlet = cmdlet;
                CancelSource = cancellation;
            }

            public PowerShell PowerShell { get; }

            public Runspace Runspace { get; }

            public ThreadController PipelineThread { get; }

            public PSCmdlet Cmdlet { get; }

            public CancellationTokenSource CancelSource { get; }

            public static async Task<RunspaceHandle> CreateAsync(CancellationToken cancellationToken)
            {
                InitialSessionState iss = InitialSessionState.CreateDefault();
                Runspace runspace = RunspaceFactory.CreateRunspace(iss);
                runspace.Open();
                PowerShell pwsh = PowerShell.Create();
                pwsh.Runspace = runspace;
                pwsh.AddScript(@"
                    [CmdletBinding()]
                    param([MulticastDelegate] $Delegate)
                    end {
                        $Delegate.Invoke($PSCmdlet, $ExecutionContext)
                    }");

                var completed = new TaskCompletionSource<(PSCmdlet, ThreadController)>();
                var controllerCancel = new CancellationTokenSource();
                if (cancellationToken.CanBeCanceled)
                {
                    controllerCancel = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                }

                pwsh.AddParameter("Delegate", new Action<PSCmdlet, EngineIntrinsics>(
                    (cmdlet, engine) =>
                    {
                        var threadController = new ThreadController(engine, cmdlet);
                        Task.Run(() => completed.TrySetResult((cmdlet, threadController)));
                        threadController.GiveControl(controllerCancel.Token);
                    }));

                pwsh.BeginInvoke();
                var (psCmdlet, controller) = await completed.Task;
                return new RunspaceHandle(pwsh, runspace, controller, psCmdlet, controllerCancel);
            }

            public void Deconstruct(
                out PowerShell powerShell,
                out Runspace runspace,
                out ThreadController controller,
                out PSCmdlet cmdlet)
            {
                powerShell = PowerShell;
                runspace = Runspace;
                controller = PipelineThread;
                cmdlet = Cmdlet;
            }

            public void Dispose() => Dispose(true);

            protected virtual void Dispose(bool disposing)
            {
                if (_isDisposed)
                {
                    return;
                }

                if (disposing)
                {
                    CancelSource?.Cancel();
                    CancelSource?.Dispose();
                    PowerShell?.Dispose();
                    Runspace?.Dispose();
                }

                _isDisposed = true;
            }
        }

        private readonly struct ParsedScript
        {
            public readonly Ast Ast;

            public readonly Token[] Tokens;

            public readonly IScriptPosition CursorPosition;

            public readonly IScriptExtent Selection;

            public ParsedScript(Ast ast, Token[] tokens, IScriptPosition cursorPosition, IScriptExtent selection)
            {
                Ast = ast;
                Tokens = tokens;
                CursorPosition = cursorPosition;
                Selection = selection;
            }

            public void Deconstruct(out Ast ast, out Token[] tokens, out IScriptPosition cursor, out IScriptExtent selection)
            {
                ast = Ast;
                tokens = Tokens;
                cursor = CursorPosition;
                selection = Selection;
            }
        }
    }
}
