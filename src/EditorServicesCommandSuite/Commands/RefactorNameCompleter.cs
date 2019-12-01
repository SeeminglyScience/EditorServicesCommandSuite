using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.Commands
{
    /// <summary>
    /// Provides argument completion for Command Suite refactor provider names.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class RefactorNameCompleter : IArgumentCompleter
    {
        [ThreadStatic]
        private static RefactorProviderInfo[] s_providerInfoCache;

        /// <summary>
        /// Called by PowerShell to complete arguments for Get-RefactorOption cmdlet.
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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IEnumerable<CompletionResult> CompleteArgument(
            string commandName,
            string parameterName,
            string wordToComplete,
            CommandAst commandAst,
            IDictionary fakeBoundParameters)
        {
            if (string.IsNullOrEmpty(wordToComplete))
            {
                return GetRefactorOptions().Select(ToCompletionResult);
            }

            var pattern = new WildcardPattern(wordToComplete + "*", WildcardOptions.IgnoreCase);
            return GetRefactorOptions()
                .Where(i => pattern.IsMatch(i.DisplayName))
                .Select(ToCompletionResult);
        }

        internal static RefactorProviderInfo[] GetRefactorOptions(PSCmdlet errorContext = null)
        {
            if (s_providerInfoCache != null)
            {
                return s_providerInfoCache;
            }

            if (!CommandSuite.TryGetInstance(out CommandSuite suite))
            {
                var exception = new NoCommandSuiteInstanceException();
                errorContext?.ThrowTerminatingError(
                    new ErrorRecord(
                        exception.ErrorRecord,
                        exception));
                return Array.Empty<RefactorProviderInfo>();
            }

            IDocumentRefactorProvider[] providers = suite.Refactors.GetProviders();
            s_providerInfoCache = new RefactorProviderInfo[providers.Length];
            for (int i = 0; i < providers.Length; i++)
            {
                RefactorAttribute refactorAttribute = providers[i]
                    .GetType()
                    .GetCustomAttribute<RefactorAttribute>(inherit: true);

                string commandName = string.Join(
                    Symbols.Dash.ToString(),
                    refactorAttribute.Verb,
                    refactorAttribute.Noun);

                FunctionInfo command = (FunctionInfo)suite.ExecutionContext.SessionState.InvokeCommand
                    .GetCommand(
                        commandName,
                        CommandTypes.Function);

                s_providerInfoCache[i] = new RefactorProviderInfo(
                    providers[i],
                    command);
            }

            return s_providerInfoCache;
        }

        private static CompletionResult ToCompletionResult(RefactorProviderInfo info)
        {
            return new CompletionResult(
                info.DisplayName,
                info.DisplayName,
                CompletionResultType.ParameterValue,
                info.Description);
        }
    }
}
