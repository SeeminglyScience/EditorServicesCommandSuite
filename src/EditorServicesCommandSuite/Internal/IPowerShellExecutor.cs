using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides the ability to invoke PowerShell commands in a way that will not
    /// conflict with the host editor.
    /// </summary>
    public interface IPowerShellExecutor
    {
        /// <summary>
        /// Executes a PowerShell command in a way that will not conflict with the
        /// host editor.
        /// </summary>
        /// <param name="psCommand">The command to invoke.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <typeparam name="TResult">The return type.</typeparam>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain the result of the invocation.
        /// </returns>
         Task<IEnumerable<TResult>> ExecuteCommandAsync<TResult>(
             PSCommand psCommand,
             CancellationToken cancellationToken);

        /// <summary>
        /// Executes a PowerShell command in a way that will not conflict with the
        /// host editor.
        /// </summary>
        /// <param name="psCommand">The command to invoke.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <typeparam name="TResult">The return type.</typeparam>
        /// <returns>
        /// The result of the invocation.
        /// </returns>
         IEnumerable<TResult> ExecuteCommand<TResult>(
             PSCommand psCommand,
             CancellationToken cancellationToken);
    }
}
