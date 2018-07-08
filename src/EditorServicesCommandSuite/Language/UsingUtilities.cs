using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Language
{
    internal static class UsingUtilities
    {
        private static UsingStatementsConfiguration _config;

        private static UsingStatementsConfiguration Config
        {
            get
            {
                if (_config != null)
                {
                    return _config;
                }

                _config = new UsingStatementsConfiguration();
                Settings.Main.Changed +=
                    (e, args) => _config = new UsingStatementsConfiguration();

                return _config;
            }
        }

        public static string GetUsingStatementString(IEnumerable<UsingDescription> usings)
        {
            var sb = new StringBuilder();
            var first = true;
            foreach (var group in GetUsingGroups(usings))
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    if (Config.SeparateGroupsWithNewLine)
                    {
                        sb.AppendLine();
                    }

                    sb.AppendLine();
                }

                var sortedGroup =
                    Config.SystemNamespaceFirst
                        ? group
                            .OrderByDescending(u => u.Text.StartsWith("System"))
                            .ThenBy(u => u.Text)
                        : group.OrderBy(u => u.Text);

                sb.Append(string.Join(Settings.NewLine, sortedGroup));
            }

            return sb.ToString();
        }

        private static IEnumerable<IGrouping<UsingStatementKind, UsingDescription>> GetUsingGroups(IEnumerable<UsingDescription> usings)
        {
            return usings
                .GroupBy(u => u.Kind)
                .OrderBy(ug => Array.IndexOf(Config.UsingKindOrder, ug.Key));
        }

        private class UsingStatementsConfiguration : RefactorConfiguration
        {
            [DefaultFromSettingAttribute("UsingStatements.SeparateGroupsWithNewLine", Default = "$true")]
            public bool SeparateGroupsWithNewLine { get; set; } = true;

            [DefaultFromSettingAttribute("UsingStatements.SystemNamespaceFirst", Default = "$true")]
            public bool SystemNamespaceFirst { get; set; } = true;

            [DefaultFromSettingAttribute("UsingStatements.UsingKindOrder", Default = "'Assembly', 'Module', 'Namespace'")]
            public UsingStatementKind[] UsingKindOrder { get; set; } = new UsingStatementKind[]
            {
                UsingStatementKind.Assembly,
                UsingStatementKind.Module,
                UsingStatementKind.Namespace,
            };
        }
    }
}
