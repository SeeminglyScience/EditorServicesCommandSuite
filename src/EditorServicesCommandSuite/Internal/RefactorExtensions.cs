using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides convenience overloads for refactor interface methods.
    /// </summary>
    public static class RefactorExtensions
    {
        /// <summary>
        /// Shows a warning message.
        /// </summary>
        /// <param name="ui">The refactor UI interface.</param>
        /// <param name="message">The message to show.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
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
        public static IEnumerable<TResult> ExecuteCommand<TResult>(
            this IPowerShellExecutor executor,
            PSCommand psCommand)
        {
            return executor.ExecuteCommand<TResult>(psCommand, CancellationToken.None);
        }
    }
}
