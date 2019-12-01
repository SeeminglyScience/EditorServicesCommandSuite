using System;
using System.Collections.Concurrent;
using System.IO;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.Utility
{
    internal sealed class ManifestInfo : PowerShellDataFile
    {
        private ManifestInfo(string resolvedPath)
            : base(resolvedPath)
        {
        }

        internal string ModuleName => Path.GetFileNameWithoutExtension(FilePath);

        internal string RootModule => GetField<string>(nameof(RootModule));

        internal Version ModuleVersion => GetField<Version>(nameof(ModuleVersion));

        internal string Author => GetField<string>(nameof(Author));

        internal string CompanyName => GetField<string>(nameof(CompanyName));

        internal string Copyright => GetField<string>(nameof(Copyright));

        internal string Description => GetField<string>(nameof(Description));

        internal string PowerShellVersion => GetField<string>(nameof(PowerShellVersion));

        internal string DotNetFrameworkVersion => GetField<string>(nameof(DotNetFrameworkVersion));

        internal string CLRVersion => GetField<string>(nameof(CLRVersion));

        internal string ProcessorArchitecture => GetField<string>(nameof(ProcessorArchitecture));

        internal string RequiredModules => GetField<string>(nameof(RequiredModules));

        internal string[] FunctionsToExport => GetArrayField<string>(nameof(FunctionsToExport));

        internal string[] CmdletsToExport => GetArrayField<string>(nameof(CmdletsToExport));

        internal string[] VariablesToExport => GetArrayField<string>(nameof(VariablesToExport));

        internal string[] AliasesToExport => GetArrayField<string>(nameof(AliasesToExport));

        internal string[] FileList => GetArrayField<string>(nameof(FileList));

        internal System.Collections.Hashtable PrivateData => GetField<System.Collections.Hashtable>(nameof(PrivateData));

        internal static bool TryGetWorkspaceManifest(IRefactorWorkspace workspace, out ManifestInfo manifestInfo)
        {
            if (workspace == null)
            {
                manifestInfo = null;
                return false;
            }

            string sourcePath = Settings.SourceManifestPath;
            if (string.IsNullOrEmpty(sourcePath))
            {
                manifestInfo = null;
                return false;
            }

            workspace.TryResolveRelativePath(sourcePath, out bool doesExist, out string manifestPath);
            if (!doesExist)
            {
                manifestInfo = null;
                return false;
            }

            manifestInfo = GetOrCreate(
                manifestPath,
                _ => new ManifestInfo(manifestPath));

            return true;
        }
    }
}
