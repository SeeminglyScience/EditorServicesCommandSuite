using System;
using System.Globalization;
using System.IO;
using System.Management.Automation;

namespace EditorServicesCommandSuite.Utility
{
    internal static class Error
    {
        public static ErrorRecord UntitledWorkspaceNotSupported()
        {
            return new ErrorRecord(
                new PSNotSupportedException(RefactorStrings.UntitledWorkspaceNotSupported),
                nameof(UntitledWorkspaceNotSupported),
                ErrorCategory.InvalidArgument,
                null);
        }

        public static ErrorRecord UntitledWorkspaceNotSupported(string message)
        {
            return new ErrorRecord(
                new PSNotSupportedException(message),
                nameof(UntitledWorkspaceNotSupported),
                ErrorCategory.InvalidArgument,
                null);
        }

        public static ErrorRecord InvalidScopeNoPath(SettingsScope scope)
        {
            return new ErrorRecord(
                new PSArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        SettingsFileStrings.InvalidScopeNoPath,
                        scope)),
                nameof(SettingsFileStrings.InvalidScopeNoPath),
                ErrorCategory.InvalidArgument,
                scope);
        }

        public static ErrorRecord ReadWriteFileNotFileSystemProvider(object actualProvider)
        {
            return new ErrorRecord(
                ReadWriteFileNotFileSystemProviderEx(actualProvider),
                nameof(PathStrings.ReadWriteFileNotFileSystemProvider),
                ErrorCategory.InvalidArgument,
                actualProvider);
        }

        public static PSInvalidOperationException ReadWriteFileNotFileSystemProviderEx(object actualProvider)
        {
            return new PSInvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.ReadWriteFileNotFileSystemProvider,
                    actualProvider));
        }

        public static ErrorRecord FileAlreadyExistsNoForce(string path)
        {
            return new ErrorRecord(
                FileAlreadyExistsNoForceEx(path),
                nameof(PathStrings.FileAlreadyExistsNoForce),
                ErrorCategory.WriteError,
                path);
        }

        public static IOException FileAlreadyExistsNoForceEx(string path)
        {
            return new IOException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.FileAlreadyExistsNoForce,
                    path));
        }

        public static ErrorRecord ParentMissingNoForce(string path)
        {
            return new ErrorRecord(
                ParentMissingNoForceEx(path),
                nameof(PathStrings.ParentDirectoryMissingNoForce),
                ErrorCategory.WriteError,
                path);
        }

        public static IOException ParentMissingNoForceEx(string path)
        {
            return new IOException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.ParentDirectoryMissingNoForce,
                    path));
        }

        public static ErrorRecord Wrap(Exception e)
        {
            if (e is RuntimeException rte)
            {
                return Wrap(rte);
            }

            return new ErrorRecord(
                e,
                "CommandSuiteUnexpectedError",
                ErrorCategory.NotSpecified,
                null);
        }

        public static ErrorRecord Wrap(RuntimeException rte)
        {
            return new ErrorRecord(rte.ErrorRecord, rte);
        }
    }
}
