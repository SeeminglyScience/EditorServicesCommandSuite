using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.Utility
{
    internal class SettingsContext : IDisposable
    {
        private static readonly Hashtable s_emptyHashtable = new Hashtable();

        private readonly bool _isProcessScope;

        internal SettingsContext()
        {
            _isProcessScope = true;
            RawSettings = s_emptyHashtable;
            FilePath = string.Empty;
            return;
        }

        internal SettingsContext(string path)
        {
            FilePath = path;
            if (!Path.IsPathRooted(path) || path.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                RawSettings = s_emptyHashtable;
                return;
            }

            var directory = Path.GetDirectoryName(FilePath);
            if (!Directory.Exists(directory))
            {
                RawSettings = s_emptyHashtable;
                return;
            }

            ParseDataFileHashtable();
            Watcher = new FileSystemWatcher(
                directory,
                Path.GetFileName(FilePath));

            Watcher.Changed += OnFileChanged;
            Watcher.Created += OnFileChanged;
            Watcher.Deleted += OnFileChanged;
            Watcher.Renamed += OnFileChanged;
            Watcher.NotifyFilter = NotifyFilters.LastWrite;
            Watcher.EnableRaisingEvents = true;
        }

        internal event EventHandler Changed;

        private string FilePath { get; set; }

        private Hashtable RawSettings { get; set; }

        private FileSystemWatcher Watcher { get; set; }

        public void Dispose()
        {
            Watcher?.Dispose();
        }

        internal void SetSetting(string key, object value)
        {
            RawSettings[key] = value;
        }

        internal bool TryGetSettingPath(string key, out object value)
        {
            if (_isProcessScope)
            {
                value = RawSettings[key];
                return RawSettings.ContainsKey(key);
            }

            var source = RawSettings;
            if (source == s_emptyHashtable)
            {
                value = null;
                return false;
            }

            var parts = key.Split('.');
            for (var i = 0; i < parts.Length; i++)
            {
                if (!source.ContainsKey(parts[i]))
                {
                    value = null;
                    return false;
                }

                if (i == parts.Length - 1)
                {
                    value = source[parts[i]];
                    return true;
                }

                source = source[parts[i]] as Hashtable;
                if (source == null)
                {
                    value = null;
                    return false;
                }
            }

            value = null;
            return false;
        }

        private void OnFileChanged(object sender, EventArgs e)
        {
            ParseDataFileHashtable();
            Changed?.Invoke(this, EventArgs.Empty);
        }

        private void ParseDataFileHashtable()
        {
            if (!File.Exists(FilePath))
            {
                RawSettings = s_emptyHashtable;
            }

            if (!Path.GetExtension(FilePath).Equals(".psd1", StringComparison.OrdinalIgnoreCase))
            {
                RawSettings = s_emptyHashtable;
            }

            var ast = Parser.ParseFile(FilePath, out _, out ParseError[] errors);
            if (errors.Any())
            {
                RawSettings = s_emptyHashtable;
            }

            RawSettings = ast.FindAst<HashtableAst>()?.SafeGetValue() as Hashtable ?? s_emptyHashtable;
        }
    }
}
