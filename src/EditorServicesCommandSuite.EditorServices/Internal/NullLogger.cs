using System;
using Microsoft.PowerShell.EditorServices;
using Microsoft.PowerShell.EditorServices.Utility;

namespace EditorServicesCommandSuite.EditorServices.Internal
{
    /// <summary>
    /// Provides a no-op <see cref="ILogger" /> implementation.
    /// </summary>
    public class NullLogger : ILogger
    {
        /// <summary>
        /// Gets a null instance of <see cref="ILogger" /> used for creating instances
        /// of <see cref="PowerShellContext" />.
        /// </summary>
        public static ILogger Instance { get; } = new NullLogger();

        /// <summary>
        /// Gets the minimum configured log level.
        /// </summary>
        LogLevel ILogger.MinimumConfiguredLogLevel { get; }

        /// <summary>
        /// A no-op implemention of <see cref="ILogger.Write" />.
        /// </summary>
        /// <param name="logLevel">The parameter is not used.</param>
        /// <param name="logMessage">The parameter is not used.</param>
        /// <param name="callerName">The parameter is not used.</param>
        /// <param name="callerSourceFile">The parameter is not used.</param>
        /// <param name="callerLineNumber">The parameter is not used.</param>
        void ILogger.Write(
            LogLevel logLevel,
            string logMessage,
            string callerName,
            string callerSourceFile,
            int callerLineNumber)
        {
        }

        /// <summary>
        /// A no-op implemention of <see cref="ILogger.WriteException" />.
        /// </summary>
        /// <param name="errorMessage">The parameter is not used.</param>
        /// <param name="errorException">The parameter is not used.</param>
        /// <param name="callerName">The parameter is not used.</param>
        /// <param name="callerSourceFile">The parameter is not used.</param>
        /// <param name="callerLineNumber">The parameter is not used.</param>
        void ILogger.WriteException(
            string errorMessage,
            Exception errorException,
            string callerName,
            string callerSourceFile,
            int callerLineNumber)
        {
        }

        /// <summary>
        /// A no-op implemention of <see cref="IDisposable.Dispose" />.
        /// </summary>
        void IDisposable.Dispose()
        {
        }

        /// <summary>
        /// A no-op implemention of <see cref="ILogger.WriteHandledException" />.
        /// </summary>
        /// <param name="errorMessage">The parameter is not used.</param>
        /// <param name="exception">The parameter is not used.</param>
        /// <param name="callerName">The parameter is not used.</param>
        /// <param name="callerSourceFile">The parameter is not used.</param>
        /// <param name="callerLineNumber">The parameter is not used.</param>
        void ILogger.WriteHandledException(
            string errorMessage,
            Exception exception,
            string callerName,
            string callerSourceFile,
            int callerLineNumber)
        {
        }
    }
}
