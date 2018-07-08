using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Commands
{
    /// <summary>
    /// Provides the cmdlet Get-CommandSuiteSetting.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "CommandSuiteSetting", DefaultParameterSetName = AllParameterSets)]
    [OutputType(typeof(CommandSuiteSettingInfo))]
    public class GetCommandSuiteSettingCommand : PSCmdlet
    {
        private const string NameParameterSet = "ByName";

        private const string FullNameParameterSet = "ByFullName";

        private const string AllParameterSets = "__AllParameterSets";

        private CommandSuiteSettingInfo[] _settings;

        private HashSet<string> _alreadyProcessed = new HashSet<string>();

        /// <summary>
        /// Gets or sets the name of the setting to get.
        /// </summary>
        [Parameter(Position = 0, ParameterSetName = NameParameterSet)]
        [SupportsWildcards]
        [ValidateNotNullOrEmpty]
        [ArgumentCompleter(typeof(CommandSuiteSettingCompleter))]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the full name including group of the setting to get.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true, ParameterSetName = FullNameParameterSet)]
        [SupportsWildcards]
        [ValidateNotNullOrEmpty]
        [ArgumentCompleter(typeof(CommandSuiteSettingCompleter))]
        public string FullName { get; set; }

        /// <summary>
        /// The BeginProcessing method.
        /// </summary>
        protected override void BeginProcessing()
        {
            _settings = Settings.GetAllSettings().ToArray();
        }

        /// <summary>
        /// The ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (ParameterSetName.Equals(AllParameterSets, StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }

            WildcardPattern pattern;
            if (ParameterSetName.Equals(NameParameterSet, StringComparison.CurrentCultureIgnoreCase))
            {
                pattern = new WildcardPattern(Name, WildcardOptions.IgnoreCase);
                WriteObject(
                    _settings.Where(
                        setting =>
                            pattern.IsMatch(setting.Name) &&
                            _alreadyProcessed.Add(setting.FullName)),
                    enumerateCollection: true);
                return;
            }

            pattern = new WildcardPattern(FullName, WildcardOptions.IgnoreCase);
            WriteObject(
                _settings.Where(
                    setting =>
                        pattern.IsMatch(setting.FullName) &&
                        _alreadyProcessed.Add(setting.FullName)),
                enumerateCollection: true);
        }

        /// <summary>
        /// The EndProcessing method.
        /// </summary>
        protected override void EndProcessing()
        {
            if (MyInvocation.ExpectingInput ||
                !ParameterSetName.Equals(AllParameterSets, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            WriteObject(
                _settings,
                enumerateCollection: true);
        }
    }
}
