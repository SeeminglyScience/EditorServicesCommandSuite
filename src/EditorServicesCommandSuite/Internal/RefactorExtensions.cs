using System.Collections.Generic;
using System.ComponentModel;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides convenience overloads for refactor interface methods.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class RefactorExtensions
    {
        /// <summary>
        /// Uses the editor host UI to prompt the user for a string.
        /// </summary>
        /// <param name="ui">The refactor UI interface.</param>
        /// <param name="caption">The caption for the prompt.</param>
        /// <param name="message">The message for the prompt.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain the input string.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<string> ShowInputPromptAsync(
            this IRefactorUI ui,
            string caption,
            string message)
        {
            return ui.ShowInputPromptAsync(caption, message, waitForResponse: true);
        }

        /// <summary>
        /// Shows a warning message.
        /// </summary>
        /// <param name="ui">The refactor UI interface.</param>
        /// <param name="message">The message to show.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task ShowWarningMessageAsync(this IRefactorUI ui, string message)
        {
            return ui.ShowWarningMessageAsync(message, waitForResponse: false);
        }

        /// <summary>
        /// Shows a error message.
        /// </summary>
        /// <param name="ui">The refactor UI interface.</param>
        /// <param name="message">The message to show.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task ShowErrorMessageAsync(this IRefactorUI ui, string message)
        {
            return ui.ShowErrorMessageAsync(message, waitForResponse: false);
        }

        /// <summary>
        /// Gets diagnostic markers for a specific document.
        /// </summary>
        /// <param name="analysis">The refactor analysis interface.</param>
        /// <param name="path">The path of the document.</param>
        /// <returns>The active diagnostic markers.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IEnumerable<DiagnosticMarker> GetDiagnosticsFromPath(
            this IRefactorAnalysisContext analysis,
            string path)
        {
            return analysis.GetDiagnosticsFromPath(path, CancellationToken.None);
        }

        /// <summary>
        /// Gets diagnostic markers for the contents of an untitled document.
        /// </summary>
        /// <param name="analysis">The refactor analysis interface.</param>
        /// <param name="contents">The text of the document to analyze.</param>
        /// <returns>The active diagnostic markers.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IEnumerable<DiagnosticMarker> GetDiagnosticsFromContents(
            this IRefactorAnalysisContext analysis,
            string contents)
        {
            return analysis.GetDiagnosticsFromContents(contents, CancellationToken.None);
        }

        /// <summary>
        /// Gets diagnostic markers for a specific document.
        /// </summary>
        /// <param name="analysis">The refactor analysis interface.</param>
        /// <param name="path">The path of the document.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain active diagnostic markers.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromPathAsync(
            this IRefactorAnalysisContext analysis,
            string path)
        {
            return analysis.GetDiagnosticsFromPathAsync(path, CancellationToken.None);
        }

        /// <summary>
        /// Gets diagnostic markers for the contents of an untitled document.
        /// </summary>
        /// <param name="analysis">The refactor analysis interface.</param>
        /// <param name="contents">The text of the document to analyze.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain active diagnostic markers.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromContentsAsync(
            this IRefactorAnalysisContext analysis,
            string contents)
        {
            return analysis.GetDiagnosticsFromContentsAsync(contents, CancellationToken.None);
        }

        /// <summary>
        /// Executes a PowerShell command in a way that will not conflict with the
        /// host editor.
        /// </summary>
        /// <param name="executor">The refactor PowerShell execution interface.</param>
        /// <param name="psCommand">The command to invoke.</param>
        /// <typeparam name="TResult">The return type.</typeparam>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain the result of the invocation.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<IEnumerable<TResult>> ExecuteCommandAsync<TResult>(
            this IPowerShellExecutor executor,
            PSCommand psCommand)
        {
            return executor.ExecuteCommandAsync<TResult>(psCommand, CancellationToken.None);
        }

        /// <summary>
        /// Executes a PowerShell command in a way that will not conflict with the
        /// host editor.
        /// </summary>
        /// <param name="executor">The refactor PowerShell execution interface.</param>
        /// <param name="psCommand">The command to invoke.</param>
        /// <typeparam name="TResult">The return type.</typeparam>
        /// <returns>The result of the invocation.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IEnumerable<TResult> ExecuteCommand<TResult>(
            this IPowerShellExecutor executor,
            PSCommand psCommand)
        {
            return executor.ExecuteCommand<TResult>(psCommand, CancellationToken.None);
        }
    }
}
