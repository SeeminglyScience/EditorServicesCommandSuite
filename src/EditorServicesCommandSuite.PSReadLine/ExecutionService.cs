using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal class ExecutionService : IPowerShellExecutor
    {
        public IEnumerable<TResult> ExecuteCommand<TResult>(PSCommand psCommand, CancellationToken cancellationToken)
        {
            using (var pwsh = PowerShell.Create(RunspaceMode.CurrentRunspace))
            using (cancellationToken.Register(() => pwsh.BeginStop(null, null)))
            {
                pwsh.Commands = psCommand;
                return pwsh.Invoke<TResult>();
            }
        }

        public Task<IEnumerable<TResult>> ExecuteCommandAsync<TResult>(PSCommand psCommand, CancellationToken cancellationToken)
        {
            return Task.FromResult(ExecuteCommand<TResult>(psCommand, cancellationToken));
        }
    }
}
