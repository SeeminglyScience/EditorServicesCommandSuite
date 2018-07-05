using System.Management.Automation;
using System.Management.Automation.Internal;

namespace EditorServicesCommandSuite.Utility
{
    internal static class MiscExtensions
    {
        internal static void ThrowIfStopping(this PSCmdlet cmdlet)
        {
            if (cmdlet == null || !cmdlet.Stopping)
            {
                return;
            }

            throw new PipelineStoppedException();
        }

        internal static PSObject Invoke(this PSObject pso, string methodName, params object[] args)
        {
            return PSObject.AsPSObject(
                pso.Methods[methodName]?.Invoke(args)
                ?? AutomationNull.Value);
        }
    }
}
