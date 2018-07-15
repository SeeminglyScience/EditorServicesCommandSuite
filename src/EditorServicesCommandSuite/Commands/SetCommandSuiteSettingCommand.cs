using System;
using System.Linq;
using System.Management.Automation;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Commands
{
    /// <summary>
    /// Provides the Set-CommandSuiteSetting cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, CommandSuiteSetting, DefaultParameterSetName = SettingInfoParameterSet)]
    public class SetCommandSuiteSettingCommand : PSCmdlet
    {
        private const string SettingInfoTypeName = "EditorServicesCommandSuite.Utility.CommandSuiteSettingInfo";

        private const string CommandSuiteSetting = "CommandSuiteSetting";

        private const string FullNameParameterSet = "ByFullName";

        private const string SettingInfoParameterSet = "BySettingInfo";

        /// <summary>
        /// Gets or sets the setting info object to change.
        /// </summary>
        [Parameter(ValueFromPipeline = true, Mandatory = true, ParameterSetName = SettingInfoParameterSet)]
        [PSTypeName(SettingInfoTypeName)]
        [ValidateNotNull]
        [Alias("InputObject")]
        public PSObject SettingInfo { get; set; }

        /// <summary>
        /// Gets or sets the full name of the setting to change.
        /// </summary>
        [Parameter(
            Position = 0,
            Mandatory = true,
            ValueFromPipeline = true,
            ParameterSetName = FullNameParameterSet)]
        [ArgumentCompleter(typeof(CommandSuiteSettingCompleter))]
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the value use when changing the setting.
        /// </summary>
        [Parameter(Mandatory = true)]
        [AllowNull]
        [AllowEmptyCollection]
        [AllowEmptyString]
        public PSObject Value { get; set; }

        /// <summary>
        /// The ProcessRecord method.
        /// </summary>
        protected override void ProcessRecord()
        {
            string fullName;
            CommandSuiteSettingInfo settingInfo;
            if (ParameterSetName.Equals(FullNameParameterSet, StringComparison.OrdinalIgnoreCase))
            {
                fullName = FullName;
            }
            else
            {
                // If the setting info object is a real CommandSuiteSettingInfo object then
                // use the Value property to set it. If it's something else (like a deserialized
                // PSObject) then just use the full name to retrieve the real object.
                settingInfo = SettingInfo.BaseObject as CommandSuiteSettingInfo;
                if (settingInfo != null)
                {
                    SetSetting(settingInfo);
                    return;
                }

                fullName = SettingInfo.Properties["FullName"]?.Value?.ToString() ?? string.Empty;
            }

            settingInfo = Settings
                .GetAllSettings()
                .FirstOrDefault(setting => setting.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));

            if (settingInfo != null)
            {
                SetSetting(settingInfo);
                return;
            }
        }

        private void SetSetting(CommandSuiteSettingInfo settingInfo)
        {
            try
            {
                settingInfo.Value = Value;
            }
            catch (PSInvalidCastException exception)
            {
                // The value property isn't typed but does use LanguagePrimitives to
                // convert passed values.
                WriteError(
                    new ErrorRecord(
                        exception,
                        nameof(PSInvalidCastException),
                        ErrorCategory.InvalidArgument,
                        Value));
            }
        }
    }
}
