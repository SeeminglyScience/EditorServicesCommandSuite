using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Tests
{
    public class MockContext : IDisposable
    {
        private readonly PowerShell _pwsh;

        private readonly Runspace _runspace;

        private readonly ThreadController _threadController;

        private readonly CancellationTokenSource _controllerCancel;

        private readonly PSCmdlet _cmdlet;

        private bool _isDisposed;

        private MockContext(
            PowerShell powerShell,
            Runspace runspace,
            ThreadController pipelineThread,
            PSCmdlet cmdlet,
            CancellationTokenSource controllerCancel)
        {
            _pwsh = powerShell;
            _runspace = runspace;
            _threadController = pipelineThread;
            _cmdlet = cmdlet;
            _controllerCancel = controllerCancel;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _controllerCancel.Cancel();
            _pwsh?.Dispose();
            _runspace?.Dispose();
            _isDisposed = true;
        }

        internal static async Task<MockContext> CreateAsync(
            bool withRunspace = false,
            CancellationToken cancellationToken = default)
        {
            if (withRunspace)
            {
                return await CreateWithRunspaceAsync(cancellationToken);
            }

            return await CreateBasicAsync(cancellationToken);
        }

        private static Task<MockContext> CreateBasicAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(
                new MockContext(
                    powerShell: null,
                    runspace: null,
                    pipelineThread: null,
                    cmdlet: null,
                    controllerCancel: CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)));
        }

        private static async Task<MockContext> CreateWithRunspaceAsync(CancellationToken cancellationToken)
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
            return new MockContext(pwsh, runspace, controller, psCmdlet, controllerCancel);
        }

        internal static async Task<string> GetRefactoredTextAsync(
            string testString,
            Func<DocumentContextBase, Task<IEnumerable<DocumentEdit>>> editFactory,
            bool withRunspace = false,
            CancellationToken cancellationToken = default)
        {
            Settings.SetSetting("NewLine", "\n");
            Settings.SetSetting("TabString", "\t");
            using (MockContext mockContext = await MockContext.CreateAsync(withRunspace, cancellationToken))
            {
                var context = mockContext.GetContext(testString);
                var sb = new StringBuilder(context.RootAst.Extent.Text);

                foreach (var edit in (await editFactory(context)).OrderByDescending(e => e.StartOffset))
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

                return sb.ToString();
            }
        }

        internal DocumentContextBase GetContext(string testString)
        {
            var sb = new StringBuilder(testString);
            var cursor = testString.IndexOf("{{c}}");
            if (cursor >= 0)
            {
                sb.Remove(cursor, 5);
            }
            else
            {
                cursor = testString.Length - 1;
            }

            var selectionStart = testString.IndexOf("{{ss}}");
            var selectionEnd = 0;
            if (selectionStart >= 0)
            {
                sb.Remove(selectionStart, 6);
                selectionEnd = testString.IndexOf("{{se}}") - 6;
                sb.Remove(selectionEnd, 6);
                cursor = selectionEnd;
            }
            else
            {
                selectionStart = cursor;
                selectionEnd = cursor;
            }

            (Ast ast, Token[] tokens, IScriptPosition position) = CommandCompletion
                .MapStringInputToParsedInput(
                    sb.ToString(),
                    cursor);

            return new DocumentContext(
                (ScriptBlockAst)ast,
                ast.FindAstAt(position),
                new LinkedList<Token>(tokens).First.At(position),
                PositionUtilities.NewScriptExtent(
                    ast.Extent,
                    selectionStart,
                    selectionEnd),
                _cmdlet,
                _controllerCancel.Token,
                _threadController);
        }
    }
}
