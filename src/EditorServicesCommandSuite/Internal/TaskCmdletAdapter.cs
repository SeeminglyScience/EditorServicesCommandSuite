using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using EditorServicesCommandSuite.Utility;
using Microsoft.PowerShell.Cmdletization;

namespace EditorServicesCommandSuite.Internal
{
    internal class TaskCmdletAdapter : CmdletAdapter<object>
    {
        private static readonly Dictionary<string, string> s_emptyPrivateData =
            new Dictionary<string, string>();

        private readonly CancellationTokenSource _isStopping = new CancellationTokenSource();

        public static CancellationToken GetStoppingCancellationToken(PSCmdlet cmdlet)
        {
            Validate.IsNotNull(nameof(cmdlet), cmdlet);
            var adapter = new TaskCmdletAdapter();
            adapter.Initialize(
                cmdlet,
                "unused",
                "1.0.0",
                new System.Version(1, 0, 0, 0),
                s_emptyPrivateData);

            return adapter._isStopping.Token;
        }

        public override void StopProcessing()
        {
            _isStopping.Cancel();
        }
    }
}
