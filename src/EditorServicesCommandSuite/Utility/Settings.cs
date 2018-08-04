using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.Utility
{
    internal class Settings : IDisposable
    {
        internal const string SettingFileName = "ESCSSettings.psd1";

        internal const string SettingFileExtension = ".psd1";

        private static Lazy<Settings> s_instance = new Lazy<Settings>(CreateDefault);

        private Settings(
            string workspaceSettingFile,
            string userSettingFile,
            string machineSettingFile)
        {
            Process = new SettingsContext();
            Workspace = new SettingsContext(workspaceSettingFile);
            User = new SettingsContext(userSettingFile);
            Machine = new SettingsContext(machineSettingFile);

            Workspace.Changed += (e, args) => Changed?.Invoke(this, SettingsScope.Workspace);
            User.Changed += (e, args) => Changed?.Invoke(this, SettingsScope.User);
            Machine.Changed += (e, args) => Changed?.Invoke(this, SettingsScope.Machine);
        }

        internal event EventHandler<SettingsScope> Changed;

        internal static Settings Main => s_instance.Value;

        [Setting(nameof(MainModuleDirectory), Default = @"'.\module'")]
        internal static string MainModuleDirectory =>
            GetSetting(nameof(MainModuleDirectory), @"'.\module'");

        [Setting(nameof(SourceManifestPath), Default = @"'.\module\*.psd1'")]
        internal static string SourceManifestPath =>
            GetSetting(nameof(SourceManifestPath), @"'.\module\*.psd1'");

        [Setting(nameof(StringLocalizationManifest), Default = @"'.\module\en-US\Strings.psd1'")]
        internal static string StringLocalizationManifest =>
            GetSetting(nameof(StringLocalizationManifest), @".\module\en-US\Strings.psd1");

        [Setting(nameof(MarkdownDocsPath), Default = @"'.\docs'")]
        internal static string MarkdownDocsPath =>
            GetSetting(nameof(MarkdownDocsPath), @".\docs");

        [Setting(nameof(NewLine), Default = "[Environment]::NewLine")]
        internal static string NewLine =>
            GetSetting(nameof(NewLine), Environment.NewLine);

        [Setting(nameof(TabString), Default = "'    '")]
        internal static string TabString =>
            GetSetting(nameof(TabString), "    ");

        [Setting(nameof(EnableAutomaticNamespaceRemoval), Default = "$true")]
        internal static bool EnableAutomaticNamespaceRemoval =>
            GetSetting(nameof(EnableAutomaticNamespaceRemoval), true);

        private SettingsContext Process { get; }

        private SettingsContext Workspace { get; }

        private SettingsContext User { get; }

        private SettingsContext Machine { get; }

        public void Dispose()
        {
            Process.Dispose();
            Workspace.Dispose();
            User.Dispose();
            Machine.Dispose();
        }

        internal static Settings CreateDefault()
        {
            return new Settings(
                GetPathFromScope(SettingsScope.Workspace),
                GetPathFromScope(SettingsScope.User),
                GetPathFromScope(SettingsScope.Machine));
        }

        internal static string GetPathFromScope(SettingsScope scope)
        {
            switch (scope)
            {
                case SettingsScope.Machine:
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "EditorServicesCommandSuite",
                        "ESCSSettings.psd1");
                case SettingsScope.User:
                    return Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "EditorServicesCommandSuite",
                        "ESCSSettings.psd1");
                case SettingsScope.Workspace:
                {
                    CommandSuite commandSuite;
                    if (!CommandSuite.TryGetInstance(out commandSuite))
                    {
                        return string.Empty;
                    }

                    if (commandSuite.Workspace.IsUntitledWorkspace())
                    {
                        return string.Empty;
                    }

                    return Path.Combine(
                        commandSuite.Workspace.GetWorkspacePath(),
                        "ESCSSettings.psd1");
                }
            }

            return string.Empty;
        }

        internal static void SetSetting<TValue>(string key, TValue value)
        {
            Main.Process.SetSetting(key, value);
        }

        internal static void SetSetting(string key, object value, Type typeOfValue)
        {
            Main.Process.SetSetting(
                key,
                LanguagePrimitives.ConvertTo(value, typeOfValue));
        }

        internal static bool TrySetSetting(string key, object value, Type typeOfValue)
        {
            if (LanguagePrimitives.TryConvertTo(value, typeOfValue, out object convertedValue))
            {
                Main.Process.SetSetting(key, convertedValue);
                return true;
            }

            return false;
        }

        internal static TValue GetSetting<TValue>(string key, TValue defaultValue)
        {
            if (TryGetSetting(key, out TValue existingValue))
            {
                return existingValue;
            }

            return defaultValue;
        }

        internal static object GetSetting(string key, Type typeOfValue)
        {
            TryGetSetting(key, typeOfValue, out object value);
            return value;
        }

        internal static bool TryGetSetting(string key, Type typeOfValue, out object value)
        {
            if (Main.TryGetRawValue(key, out object rawValue))
            {
                return LanguagePrimitives.TryConvertTo(rawValue, typeOfValue, out value);
            }

            value = null;
            return false;
        }

        internal static bool TryGetSetting<TResult>(string key, out TResult value)
        {
            if (Main.TryGetRawValue(key, out object rawValue))
            {
                return LanguagePrimitives.TryConvertTo<TResult>(rawValue, out value);
            }

            value = default(TResult);
            return false;
        }

        internal static IEnumerable<CommandSuiteSettingInfo> GetAllSettings()
        {
            return typeof(Settings).Assembly.GetTypes()
                .Where(type => type.IsSubclassOf(typeof(RefactorConfiguration)))
                .Concat(new[] { typeof(Settings) })
                .SelectMany(type => type
                    .GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                    .Where(property => property.IsDefined(typeof(SettingAttribute), true)))
                .Select(property => new
                    {
                        Type = property.PropertyType,
                        Attribute = property.GetCustomAttribute<SettingAttribute>(true),
                    })
                .GroupBy(setting => setting.Attribute.Key)
                .Select(settingGroup => settingGroup.First())
                .Select(setting =>
                    new CommandSuiteSettingInfo(
                        setting.Attribute.Key,
                        setting.Type,
                        setting.Attribute.Default));
        }

        private bool TryGetRawValue(string key, out object value)
        {
            if (Process.TryGetSettingPath(key, out value))
            {
                return true;
            }

            if (Workspace.TryGetSettingPath(key, out value))
            {
                return true;
            }

            if (User.TryGetSettingPath(key, out value))
            {
                return true;
            }

            if (Machine.TryGetSettingPath(key, out value))
            {
                return true;
            }

            return false;
        }
    }
}
