using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using EditorServicesCommandSuite.CodeGeneration;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Commands
{
    /// <summary>
    /// Provides the cmdlet New-CommandSuiteSettingFile.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "CommandSuiteSettingFile", SupportsShouldProcess = true)]
    public class NewCommandSuiteSettingFileCommand : PSCmdlet, IDynamicParameters
    {
        private const string OpenParameterName = "Open";

        private const string PathParameterSet = "ByPath";

        private const string ScopeParameterSet = "ByScope";

        private string _path;

        private CancellationTokenSource _currentOperation;

        private RuntimeDefinedParameterDictionary _dynamicParameters;

        private PowerShellScriptWriter _writer;

        /// <summary>
        /// Gets or sets the destination path for the new setting file.
        /// </summary>
        [Parameter(
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true,
            Mandatory = true,
            ParameterSetName = PathParameterSet)]
        [Alias("FullName")]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        /// <summary>
        /// Gets or sets the scope to create the new file for. This is essentially a set
        /// of predefined paths for the purposes of this cmdlet.
        /// </summary>
        [Parameter(ParameterSetName = ScopeParameterSet)]
        public SettingsScope Scope { get; set; } = SettingsScope.Workspace;

        /// <summary>
        /// Gets or sets a value indicating whether parent directories should be created
        /// automatically and existing files should be automatically overriden.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Gets dynamic parameters for the cmdlet.
        /// </summary>
        /// <returns>
        /// A parameter dictionary containing the "Open" parameter if the current editor host supports
        /// opening files, otherwise it returns an empty parameter dictionary.
        /// </returns>
        public object GetDynamicParameters()
        {
            _dynamicParameters = new RuntimeDefinedParameterDictionary();
            InternalNavigationService navigation = CommandSuite.Instance?.InternalNavigation;
            if (navigation == null || !navigation.DoesSupportOpenDocument)
            {
                return _dynamicParameters;
            }

            _dynamicParameters.Add(
                OpenParameterName,
                new RuntimeDefinedParameter(
                    OpenParameterName,
                    typeof(SwitchParameter),
                    new Collection<Attribute>()
                    {
                        new ParameterAttribute()
                        {
                            ParameterSetName = "__AllParameterSets",
                            Position = int.MinValue,
                        },
                    }));
            return _dynamicParameters;
        }

        /// <summary>
        /// The ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (ParameterSetName.Equals(ScopeParameterSet, StringComparison.Ordinal) &&
                !ValidateScope())
            {
                return;
            }

            // Any errors will be thrown by our validation method if the path is not valid.
            if (!PathUtils.IsValidPathForNewFileCmdlet(
                this,
                ref _path,
                Force.IsPresent,
                isAppend: false,
                canWhatIf: true,
                defaultFileName: Settings.SettingFileName,
                requiredExtension: Settings.SettingFileExtension))
            {
                return;
            }

            // Automatically build a hashtable of all settings found in the assembly.
            _writer = new PowerShellScriptWriter();
            _writer.OpenHashtable();
            ProcessGroups(GetGroups(Settings.GetAllSettings()));
            _writer.CloseHashtable();

            bool wasSuccessful = false;
            try
            {
                if (Should.ProcessNewFile(this, _path))
                {
                    using (var stream = new FileStream(
                        _path,
                        FileMode.OpenOrCreate,
                        FileAccess.Write,
                        FileShare.ReadWrite))
                    {
                        var writer = new StreamWriter(stream);
                        writer.Write(_writer.Edits.First().NewValue);
                        writer.Flush();
                    }

                    wasSuccessful = true;
                }
            }
            catch (IOException io)
            {
                WriteError(Error.Wrap(io));
            }

            if (!wasSuccessful)
            {
                return;
            }

            if (ShouldOpenFile() && Should.ProcessOpenFile(this, _path))
            {
                _currentOperation = new CancellationTokenSource();
                CommandSuite.Instance.InternalNavigation.OpenDocument(
                    _path,
                    _currentOperation.Token);
            }
        }

        /// <summary>
        /// The StopProcessing method.
        /// </summary>
        protected override void StopProcessing()
        {
            // Will stop OpenDocument requests if the editor host supports cancellation.
            _currentOperation?.Cancel();
        }

        private IEnumerable<IGrouping<string, CommandSuiteSettingInfo>> GetGroups(
            IEnumerable<CommandSuiteSettingInfo> settings,
            int level = 1)
        {
            return settings
                .ToLookup(
                    setting => setting.NameParts.Length > level ? setting.NameParts[level - 1] : string.Empty)
                .OrderBy(settingGroup => settingGroup.Key);
        }

        private void ProcessGroups(IEnumerable<IGrouping<string, CommandSuiteSettingInfo>> group, int level = 1)
        {
            _writer.WriteEachWithSeparator(
                group.ToArray(),
                subGroup =>
                {
                    // If the key is null then it has no more nested groups and we can just
                    // write the settings now.
                    if (string.IsNullOrEmpty(subGroup.Key))
                    {
                        ProcessGrouping(subGroup, level);
                        return;
                    }

                    // There's more groups to process, so get the nest level and create a new
                    // hashtable.
                    _writer.WriteHashtableEntry(subGroup.Key, () => _writer.OpenHashtable());
                    ProcessGrouping(subGroup, level);
                    _writer.CloseHashtable();
                },
                () => _writer.WriteLines(2));
        }

        private void ProcessGrouping(IGrouping<string, CommandSuiteSettingInfo> group, int level = 1)
        {
            if (group.Key == string.Empty)
            {
                _writer.WriteEachWithSeparator(
                    group.ToArray(),
                    setting =>
                    {
                        // If we have a description for the setting then create a comment
                        // to document the feature.
                        if (!string.IsNullOrWhiteSpace(setting.Description))
                        {
                            _writer.WriteComment(setting.Description, 100);
                            _writer.WriteLine();
                        }

                        // Write settings commented out as setting them to null can
                        // change behavior.
                        _writer.WriteChars(Symbols.NumberSign, Symbols.Space);
                        _writer.WriteHashtableEntry(
                            setting.Name,
                            () => _writer.Write(setting.DefaultAsExpression));
                    },
                    () => _writer.WriteLines(2));
                return;
            }

            level++;
            ProcessGroups(GetGroups(group, level), level);
        }

        private bool ValidateScope()
        {
            if (Scope == SettingsScope.Workspace && CommandSuite.Instance.Workspace.IsUntitledWorkspace())
            {
                WriteError(
                    Error.UntitledWorkspaceNotSupported(
                        SettingsFileStrings.NewFileUntitledWorkspace));
                return false;
            }

            Path = Settings.GetPathFromScope(Scope);
            if (string.IsNullOrEmpty(Path))
            {
                WriteError(Error.InvalidScopeNoPath(Scope));
                return false;
            }

            string directory = System.IO.Path.GetDirectoryName(Path);
            if (!(Directory.Exists(directory) ||
                PathUtils.TryCreateDirectoryForValidate(this, directory, canWhatIf: true)))
            {
                return false;
            }

            return true;
        }

        private bool ShouldOpenFile()
        {
            RuntimeDefinedParameter parameter;
            if (!_dynamicParameters.TryGetValue(OpenParameterName, out parameter))
            {
                return false;
            }

            if (parameter.Value is SwitchParameter value)
            {
                return value.IsPresent;
            }

            return false;
        }
    }
}
