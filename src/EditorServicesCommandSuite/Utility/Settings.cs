using System;
using System.IO;
using System.Management.Automation;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.Utility
{
    internal enum SettingsScope
    {
        Process,

        Workspace,

        User,

        Machine,
    }

    internal class Settings : IDisposable
    {
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

        internal static string MainModuleDirectory => GetSetting(
            "MainModuleDirectory",
            @".\module");

        internal static string SourceManifestPath => GetSetting(
            "SourceManifestPath",
            @".\module\*.psd1");

        internal static string StringLocalizationManifest => GetSetting(
            "StringLocalizationManifest",
            @".\module\en-US\Strings.psd1");

        internal static string MarkdownDocsPath => GetSetting(
            "MarkdownDocsPath",
            @".\docs");

        internal static string NewLine => GetSetting(
            "NewLine",
            Environment.NewLine);

        internal static string TabString => GetSetting(
            "TabString",
            "    ");

        internal static bool EnableAutomaticNamespaceRemoval => GetSetting(
            "EnableAutomaticNamespaceRemoval",
            true);

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
            var workspacePath =
                string.IsNullOrWhiteSpace(CommandSuite.Instance?.DocumentContext?.Workspace)
                    ? string.Empty
                    : Path.Combine(
                        CommandSuite.Instance.DocumentContext.Workspace,
                        "ESCSSettings.psd1");

            var userPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EditorServicesCommandSuite",
                "ESCSSettings.psd1");

            var machinePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EditorServicesCommandSuite",
                "ESCSSettings.psd1");

            return new Settings(workspacePath, userPath, machinePath);
        }

        internal static void SetSetting<TValue>(string key, TValue value)
        {
            Main.Process.SetSetting(key, value);
        }

        internal static TValue GetSetting<TValue>(string key, TValue defaultValue)
        {
            if (TryGetSetting(key, out TValue existingValue))
            {
                return existingValue;
            }

            return defaultValue;
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
