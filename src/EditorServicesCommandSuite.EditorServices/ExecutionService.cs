using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices;
using Microsoft.PowerShell.EditorServices.Extensions;
using Microsoft.PowerShell.EditorServices.Session;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class ExecutionService : IPowerShellExecutor
    {
        private readonly Runspace _runspace;

        private readonly PowerShellContext _powerShellContext;

        internal ExecutionService(
            EditorObject psEditor,
            CommandSuite commandSuite,
            PowerShellContext powerShellContext,
            EngineIntrinsics engine)
        {
            var iss = InitialSessionState.CreateDefault2();
            iss.Variables.Add(new SessionStateVariableEntry("psEditor", psEditor, string.Empty));
            iss.Variables.Add(new SessionStateVariableEntry("CommandSuite", commandSuite, string.Empty));
            iss.Variables.Add(new SessionStateVariableEntry(
                "MainExecutionContext",
                engine,
                string.Empty));

            iss.ImportPSModule(
                new[]
                {
                    Path.Combine(
                        Path.GetDirectoryName(
                            Path.GetDirectoryName(
                                Path.GetDirectoryName(
                                    this.GetType().Assembly.Location))),
                        "EditorServicesCommandSuite.psd1"),
                    Path.Combine(
                        Path.GetDirectoryName(
                            Path.GetDirectoryName(
                                Path.GetDirectoryName(
                                    typeof(PowerShellContext).Assembly.Location))),
                        "Commands",
                        "PowerShellEditorServices.Commands.psd1"),
                });

            _runspace = RunspaceFactory.CreateRunspace(iss);
            _runspace.Open();

            _powerShellContext = powerShellContext;
            _powerShellContext.Initialize(
                new ProfilePaths(string.Empty, string.Empty, string.Empty),
                _runspace,
                true);
        }

        public IEnumerable<TResult> ExecuteCommand<TResult>(
            PSCommand psCommand,
            CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(() => _powerShellContext.AbortExecution()))
            {
                return _powerShellContext.ExecuteCommand<TResult>(
                    psCommand,
                    null,
                    false,
                    false,
                    false)
                    .ConfigureAwait(continueOnCapturedContext: false)
                    .GetAwaiter()
                    .GetResult();
            }
        }

        public async Task<IEnumerable<TResult>> ExecuteCommandAsync<TResult>(
            PSCommand psCommand,
            CancellationToken cancellationToken)
        {
            using (cancellationToken.Register(() => _powerShellContext.AbortExecution()))
            {
                return await _powerShellContext.ExecuteCommand<TResult>(
                    psCommand,
                    null,
                    false,
                    false,
                    false);
            }
        }
    }
}
