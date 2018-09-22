using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides convenience overloads for refactor interface methods.
    /// </summary>
    internal static class RefactorExtensions
    {
        /// <summary>
        /// Creates an <see cref="Exception" /> object and passes the <see cref="Exception.Message" />
        /// property to the <see cref="IRefactorUI.ShowErrorMessageAsync(string, bool)" /> method.
        /// If <paramref name="ui" /> is <see langword="null" /> then the exception will be thrown
        /// instead.
        /// </summary>
        /// <tparam name="TException">
        /// The exception type to throw if <paramref name="ui" /> is <see langword="null" />.
        /// </tparam>
        /// <param name="ui">The refactor UI interface.</param>
        /// <param name="exceptionGenerator">The delegate that will create an exception.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        public static async Task ShowErrorMessageOrThrowAsync<TException>(
            this IRefactorUI ui,
            Func<TException> exceptionGenerator)
            where TException : Exception, IContainsErrorRecord
        {
            if (ui == null)
            {
                throw exceptionGenerator();
            }

            await ui.ShowErrorMessageAsync(exceptionGenerator().Message, waitForResponse: false);
            throw new PipelineStoppedException();
        }

        /// <summary>
        /// Creates an <see cref="Exception" /> object and passes the <see cref="Exception.Message" />
        /// property to the <see cref="IRefactorUI.ShowErrorMessageAsync(string, bool)" /> method.
        /// If <paramref name="ui" /> is <see langword="null" /> then the exception will be thrown
        /// instead.
        /// </summary>
        /// <tparam name="TArg0">
        /// The first argument type.
        /// </tparam>
        /// <tparam name="TException">
        /// The exception type to throw if <paramref name="ui" /> is <see langword="null" />.
        /// </tparam>
        /// <param name="ui">The refactor UI interface.</param>
        /// <param name="exceptionGenerator">The delegate that will create an exception.</param>
        /// <param name="arg0">The first generator argument.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        public static async Task ShowErrorMessageOrThrowAsync<TArg0, TException>(
            this IRefactorUI ui,
            Func<TArg0, TException> exceptionGenerator,
            TArg0 arg0)
            where TException : Exception, IContainsErrorRecord
        {
            if (ui == null)
            {
                throw exceptionGenerator(arg0);
            }

            await ui.ShowErrorMessageAsync(exceptionGenerator(arg0).Message, waitForResponse: false);
            throw new PipelineStoppedException();
        }

        /// <summary>
        /// Creates an <see cref="Exception" /> object and passes the <see cref="Exception.Message" />
        /// property to the <see cref="IRefactorUI.ShowErrorMessageAsync(string, bool)" /> method.
        /// If <paramref name="ui" /> is <see langword="null" /> then the exception will be thrown
        /// instead.
        /// </summary>
        /// <tparam name="TArg0">
        /// The first argument type.
        /// </tparam>
        /// <tparam name="TArg1">
        /// The second argument type.
        /// </tparam>
        /// <tparam name="TException">
        /// The exception type to throw if <paramref name="ui" /> is <see langword="null" />.
        /// </tparam>
        /// <param name="ui">The refactor UI interface.</param>
        /// <param name="exceptionGenerator">The delegate that will create an exception.</param>
        /// <param name="arg0">The first generator argument.</param>
        /// <param name="arg1">The second generator argument.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        public static async Task ShowErrorMessageOrThrowAsync<TArg0, TArg1, TException>(
            this IRefactorUI ui,
            Func<TArg0, TArg1, TException> exceptionGenerator,
            TArg0 arg0,
            TArg1 arg1)
            where TException : Exception, IContainsErrorRecord
        {
            if (ui == null)
            {
                throw exceptionGenerator(arg0, arg1);
            }

            await ui.ShowErrorMessageAsync(exceptionGenerator(arg0, arg1).Message, waitForResponse: false);
            throw new PipelineStoppedException();
        }

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
        public static Task ShowWarningMessageAsync(this IRefactorUI ui, string message)
        {
            return ui?.ShowWarningMessageAsync(message, waitForResponse: false) ?? Task.CompletedTask;
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
            return ui?.ShowErrorMessageAsync(message, waitForResponse: false) ?? Task.CompletedTask;
        }

        /// <summary>
        /// Gets diagnostic markers for a specific document.
        /// </summary>
        /// <param name="analysis">The refactor analysis interface.</param>
        /// <param name="path">The path of the document.</param>
        /// <param name="pipelineThread">
        /// The controller for the PowerShell pipeline thread.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain active diagnostic markers.
        /// </returns>
        public static Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromPathAsync(
            this IRefactorAnalysisContext analysis,
            string path,
            ThreadController pipelineThread)
        {
            return analysis.GetDiagnosticsFromPathAsync(
                path,
                pipelineThread,
                CancellationToken.None);
        }

        /// <summary>
        /// Gets diagnostic markers for the contents of an untitled document.
        /// </summary>
        /// <param name="analysis">The refactor analysis interface.</param>
        /// <param name="contents">The text of the document to analyze.</param>
        /// <param name="pipelineThread">
        /// The controller for the PowerShell pipeline thread.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain active diagnostic markers.
        /// </returns>
        public static Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromContentsAsync(
            this IRefactorAnalysisContext analysis,
            string contents,
            ThreadController pipelineThread)
        {
            return analysis.GetDiagnosticsFromContentsAsync(
                contents,
                pipelineThread,
                CancellationToken.None);
        }
    }
}
