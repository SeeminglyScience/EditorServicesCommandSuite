using System;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using Microsoft.PowerShell.Commands;

namespace EditorServicesCommandSuite.Utility
{
    internal static class PathUtils
    {
        public static readonly StringComparison PathComparision;

        public static readonly StringComparer PathComparer;

        static PathUtils()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                PathComparision = StringComparison.Ordinal;
                PathComparer = StringComparer.Ordinal;
                return;
            }

            PathComparision = StringComparison.OrdinalIgnoreCase;
            PathComparer = StringComparer.OrdinalIgnoreCase;
        }

        public static bool IsValidPathForNewFileCmdlet(
            PSCmdlet cmdlet,
            ref string path,
            bool isForce,
            bool isAppend,
            bool canWhatIf,
            string defaultFileName = null,
            string requiredExtension = null)
        {
            path = cmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                path,
                out ProviderInfo provider,
                out _);

            if (!provider.Name.Equals(FileSystemProvider.ProviderName, StringComparison.Ordinal))
            {
                cmdlet.WriteError(Error.ReadWriteFileNotFileSystemProvider(provider));
                return false;
            }

            return IsValidPathForNewItemCmdlet(
                cmdlet,
                ref path,
                isForce,
                isAppend,
                canWhatIf,
                defaultFileName,
                requiredExtension);
        }

        public static bool IsValidPathForNewItemCmdlet(
            PSCmdlet cmdlet,
            ref string path,
            bool isForce,
            bool isAppend,
            bool canWhatIf,
            string defaultFileName = null,
            string requiredExtension = null)
        {
            ValidateTargetForItemTargetHelper resultHelper = IsValidPathForNewItemCmdletImpl(
                new ValidateTargetForItemTargetHelper()
                {
                    Cmdlet = cmdlet,
                    Path = path,
                    IsForce = isForce,
                    IsAppend = isAppend,
                    ShouldDeleteCurrent = false,
                    ShouldCreateParent = false,
                    SupportsWhatIf = canWhatIf,
                    IsValid = true,
                    DefaultFileName = defaultFileName,
                    RequiredExtension = requiredExtension,
                });

            path = resultHelper.Path;
            if (!resultHelper.IsValid ||
                !(resultHelper.ShouldCreateParent || resultHelper.ShouldDeleteCurrent))
            {
                return resultHelper.IsValid;
            }

            if (resultHelper.ShouldDeleteCurrent &&
                !TryDeleteItemForValidate(cmdlet, resultHelper.Path, canWhatIf))
            {
                return false;
            }

            if (resultHelper.ShouldCreateParent)
            {
                return TryCreateDirectoryForValidate(cmdlet, resultHelper.Path, canWhatIf);
            }

            return true;
        }

        internal static bool TryDeleteItemForValidate(
            PSCmdlet cmdlet,
            string path,
            bool canWhatIf,
            bool recurse = false)
        {
            if (canWhatIf)
            {
                if (!Should.ProcessRemoveItem(cmdlet, path, out ShouldProcessReason reason))
                {
                    if (reason == ShouldProcessReason.WhatIf)
                    {
                        return true;
                    }

                    cmdlet.WriteError(Error.FileAlreadyExistsNoForce(path));
                    return false;
                }
            }

            bool wasSuccessful;
            try
            {
                cmdlet.InvokeProvider.Item.Remove(path, recurse);
                wasSuccessful = true;
            }
            catch (RuntimeException rte)
            {
                wasSuccessful = false;
                cmdlet.WriteError(new ErrorRecord(rte.ErrorRecord, rte));
            }

            return wasSuccessful;
        }

        internal static bool TryCreateDirectoryForValidate(PSCmdlet cmdlet, string path, bool canWhatIf)
        {
            if (canWhatIf)
            {
                if (!Should.ProcessCreateDirectory(cmdlet, path, out ShouldProcessReason reason))
                {
                    if (reason == ShouldProcessReason.WhatIf)
                    {
                        return true;
                    }

                    cmdlet.WriteError(Error.ParentMissingNoForce(path));
                    return false;
                }
            }

            bool wasSuccessful;
            try
            {
                cmdlet.InvokeProvider.Item.New(
                    System.IO.Path.GetDirectoryName(path),
                    System.IO.Path.GetFileName(path),
                    "Directory",
                    null);
                wasSuccessful = true;
            }
            catch (RuntimeException rte)
            {
                wasSuccessful = false;
                cmdlet.WriteError(Error.Wrap(rte));
            }

            return wasSuccessful;
        }

        private static ValidateTargetForItemTargetHelper IsValidPathForNewItemCmdletImpl(
            ValidateTargetForItemTargetHelper helper)
        {
            if (helper.Cmdlet.InvokeProvider.Item.Exists(helper.Path, force: true, literalPath: true))
            {
                if (helper.Cmdlet.InvokeProvider.Item.IsContainer(helper.Path))
                {
                    if (!string.IsNullOrEmpty(helper.DefaultFileName))
                    {
                        helper.Path = helper.Cmdlet.SessionState.Path.Combine(
                            helper.Path,
                            helper.DefaultFileName);

                        return IsValidPathForNewItemCmdletImpl(helper);
                    }

                    helper.Cmdlet.WriteError(Error.FileAlreadyExistsNoForce(helper.Path));
                    helper.IsValid = false;
                    return helper;
                }

                if (!helper.IsForce && !helper.IsAppend)
                {
                    helper.Cmdlet.WriteError(Error.FileAlreadyExistsNoForce(helper.Path));
                    helper.IsValid = false;
                    return helper;
                }

                helper.ShouldDeleteCurrent = true;
            }

            if (!string.IsNullOrEmpty(helper.RequiredExtension) &&
                !Path.GetExtension(helper.Path).Equals(helper.RequiredExtension, StringComparison.OrdinalIgnoreCase))
            {
                helper.ShouldDeleteCurrent = false;
                helper.Path = Path.ChangeExtension(helper.Path, helper.RequiredExtension);
                return IsValidPathForNewItemCmdletImpl(helper);
            }

            string directory = Path.GetDirectoryName(helper.Path);
            if (!helper.Cmdlet.InvokeProvider.Item.Exists(directory, false, true))
            {
                if (!helper.IsForce)
                {
                    helper.Cmdlet.WriteError(Error.ParentMissingNoForce(helper.Path));
                    helper.IsValid = false;
                    return helper;
                }

                helper.ShouldCreateParent = true;
            }

            return helper;
        }

        private struct ValidateTargetForItemTargetHelper
        {
            internal string Path;

            internal string RequiredExtension;

            internal string DefaultFileName;

            internal PSCmdlet Cmdlet;

            internal bool IsForce;

            internal bool IsAppend;

            internal bool SupportsWhatIf;

            internal bool ShouldCreateParent;

            internal bool ShouldDeleteCurrent;

            internal bool IsValid;
        }
    }
}
