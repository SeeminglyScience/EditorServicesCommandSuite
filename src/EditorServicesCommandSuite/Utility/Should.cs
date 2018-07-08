using System.Globalization;
using System.Management.Automation;

namespace EditorServicesCommandSuite.Utility
{
    internal static class Should
    {
        internal static bool ProcessOpenFile(PSCmdlet cmdlet, string path)
        {
            return cmdlet.ShouldProcess(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.OpenFileDescription,
                    path),
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.OpenFileWarning,
                    path),
                PathStrings.OpenFileCaption);
        }

        internal static bool ProcessOpenFile(PSCmdlet cmdlet, string path, out ShouldProcessReason reason)
        {
            return cmdlet.ShouldProcess(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.OpenFileDescription,
                    path),
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.OpenFileWarning,
                    path),
                PathStrings.OpenFileCaption,
                out reason);
        }

        internal static bool ProcessRemoveItem(PSCmdlet cmdlet, string path)
        {
            return cmdlet.ShouldProcess(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.RemoveItemDescription,
                    path),
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.RemoveItemWarning,
                    path),
                PathStrings.RemoveItemCaption);
        }

        internal static bool ProcessRemoveItem(PSCmdlet cmdlet, string path, out ShouldProcessReason reason)
        {
            return cmdlet.ShouldProcess(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.RemoveItemDescription,
                    path),
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.RemoveItemWarning,
                    path),
                PathStrings.RemoveItemCaption,
                out reason);
        }

        internal static bool ProcessCreateDirectory(PSCmdlet cmdlet, string path)
        {
            return cmdlet.ShouldProcess(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.CreateDirectoryDescription,
                    path),
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.CreateDirectoryWarning,
                    path),
                PathStrings.CreateDirectoryCaption);
        }

        internal static bool ProcessCreateDirectory(PSCmdlet cmdlet, string path, out ShouldProcessReason reason)
        {
            return cmdlet.ShouldProcess(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.CreateDirectoryDescription,
                    path),
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.CreateDirectoryWarning,
                    path),
                PathStrings.CreateDirectoryCaption,
                out reason);
        }

        internal static bool ProcessNewFile(PSCmdlet cmdlet, string path)
        {
            return cmdlet.ShouldProcess(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.CreateFileDescription,
                    path),
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.CreateFileWarning,
                    path),
                PathStrings.CreateFileCaption);
        }

        internal static bool ProcessNewFile(PSCmdlet cmdlet, string path, out ShouldProcessReason reason)
        {
            return cmdlet.ShouldProcess(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.CreateFileDescription,
                    path),
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.CreateFileWarning,
                    path),
                PathStrings.CreateFileCaption,
                out reason);
        }
    }
}
