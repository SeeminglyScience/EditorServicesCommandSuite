using System;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.Utility
{
    /// <summary>
    /// Represents an individual setting for the current Command Suite session.
    /// </summary>
    public class CommandSuiteSettingInfo
    {
        /// <summary>
        /// Contains the default value displayed by the formatter when a setting is not set.
        /// </summary>
        [Hidden, EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly string DefaultValueDisplay =
            string.Concat(
                Symbols.LessThan,
                SettingsStrings.SettingInfoDefaultValueDisplay,
                Symbols.GreaterThan);

        /// <summary>
        /// Contains the default value displayed by the formatter when a setting does not belong
        /// to a group.
        /// </summary>
        [Hidden, EditorBrowsable(EditorBrowsableState.Never)]
        public static readonly string DefaultGroupName = SettingsStrings.SettingInfoNoGroupMessage;

        private readonly string _descriptionResourceName;

        private readonly Type _typeOfValue;

        internal CommandSuiteSettingInfo(string key, Type typeOfValue, string defaultAsExpression)
        {
            _typeOfValue = typeOfValue ?? typeof(object);
            FullName = key;
            NameParts = key.Split(Symbols.Dot);

            _descriptionResourceName =
                string.Join(Symbols.Underscore.ToString(), NameParts) + "Description";

            DefaultAsExpression = string.IsNullOrWhiteSpace(defaultAsExpression)
                ? new string(Symbols.Null)
                : defaultAsExpression;

            if (NameParts.Length == 1)
            {
                Group = string.Empty;
                return;
            }

            Group = string.Join(Symbols.Dot.ToString(), NameParts.Take(NameParts.Length - 1));
        }

        /// <summary>
        /// Gets the name of the setting.
        /// </summary>
        public string Name => NameParts.Last();

        /// <summary>
        /// Gets the description of the setting.
        /// </summary>
        public string Description =>
            SettingsStrings.ResourceManager.GetString(
                string.Join(Symbols.Underscore.ToString(), NameParts) + "Description");

        /// <summary>
        /// Gets the full name including group.
        /// </summary>
        public string FullName { get; }

        /// <summary>
        /// Gets the group the setting belongs to.
        /// </summary>
        public string Group { get; }

        /// <summary>
        /// Gets or sets the current value of the setting.
        /// </summary>
        public object Value
        {
            get { return Settings.GetSetting(FullName, _typeOfValue); }
            set { Settings.SetSetting(FullName, value, _typeOfValue); }
        }

        /// <summary>
        /// Gets an array containing each part of the settings group name as
        /// well as the setting's name.
        /// </summary>
        internal string[] NameParts { get; }

        /// <summary>
        /// Gets the default value of the setting as it would be expressed in a
        /// PowerShell script.
        /// </summary>
        internal string DefaultAsExpression { get; }
    }
}
