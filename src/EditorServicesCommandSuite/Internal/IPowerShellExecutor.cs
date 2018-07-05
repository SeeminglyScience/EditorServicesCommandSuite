using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    public interface IPowerShellExecutor
    {
         Task<IEnumerable<TResult>> ExecuteCommandAsync<TResult>(
             PSCommand psCommand,
             CancellationToken cancellationToken);

         IEnumerable<TResult> ExecuteCommand<TResult>(
             PSCommand psCommand,
             CancellationToken cancellationToken);
    }
}
