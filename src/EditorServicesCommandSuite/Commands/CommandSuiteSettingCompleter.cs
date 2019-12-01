using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Commands
{
    /// <summary>
    /// Provides argument completion for Command Suite setting names.
    /// </summary>
    public class CommandSuiteSettingCompleter : IArgumentCompleter
    {
        private static CommandSuiteSettingInfo[] s_settingCache;

        /// <summary>
        /// Called by PowerShell to complete arguments for CommandSuiteSetting cmdlets.
        /// </summary>
        /// <param name="commandName">The name of the command that needs argument completion.</param>
        /// <param name="parameterName">The name of the parameter that needs argument completion.</param>
        /// <param name="wordToComplete">The (possibly empty) word being completed.</param>
        /// <param name="commandAst">The command AST in case it is needed for completion.</param>
        /// <param name="fakeBoundParameters">
        /// This parameter is similar to $PSBoundParameters, except that sometimes PowerShell
        /// cannot or will not attempt to evaluate an argument, in which case you may need to
        /// use commandAst.
        /// </param>
        /// <returns>
        /// A collection of completion results, most with ResultType set to ParameterValue.
        /// </returns>
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            var wildcardPattern = new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase);
            Func<CommandSuiteSettingInfo, bool> filterDelegate = GetFilterDelegate(wildcardPattern, parameterName);

            return GetSettingsCache()
                .Where(filterDelegate)
                .Select(setting =>
                    new CompletionResult(
                        GetCompletionValue(parameterName, setting),
                        setting.FullName,
                        CompletionResultType.ParameterValue,
                        string.IsNullOrEmpty(setting.Description) ? setting.FullName : setting.Description));
        }

        private static string GetCompletionValue(string parameterName, CommandSuiteSettingInfo setting)
        {
            if (parameterName.Equals("Name", System.StringComparison.OrdinalIgnoreCase))
            {
                return setting.Name;
            }

            return setting.FullName;
        }

        private static CommandSuiteSettingInfo[] GetSettingsCache()
        {
            return s_settingCache ?? (s_settingCache = Settings.GetAllSettings().ToArray());
        }

        private Func<CommandSuiteSettingInfo, bool> GetFilterDelegate(
            WildcardPattern pattern,
            string parameterName)
        {
            if (parameterName.Equals("FullName", StringComparison.OrdinalIgnoreCase))
            {
                return new Func<CommandSuiteSettingInfo, bool>(
                    setting => pattern.IsMatch(setting.FullName));
            }

            return new Func<CommandSuiteSettingInfo, bool>(
                setting => pattern.IsMatch(setting.Name));
        }
    }
}
