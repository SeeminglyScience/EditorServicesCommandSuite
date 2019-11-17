using System;
using Microsoft.Extensions.Logging;

namespace EditorServicesCommandSuite.EditorServices.Internal
{
    /// <summary>
    /// Provides a no-op <see cref="ILogger" /> implementation.
    /// </summary>
    public class NullLogger : ILogger
    {
        /// <summary>
        /// A no-op implemention of <see cref="ILogger.BeginScope" />.
        /// </summary>
        /// <param name="state">The parameter is not used.</param>
        /// <typeparam name="TState">The parameter is not used.</typeparam>
        /// <returns>A no-op implementation of <see cref="IDisposable" />.</returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            return NullDisposable.Value;
        }

        /// <summary>
        /// A no-op implemention of <see cref="ILogger.BeginScope" />.
        /// </summary>
        /// <param name="logLevel">The parameter is not used.</param>
        /// <returns><c>false</c>.</returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        /// <summary>
        /// A no-op implemention of <see cref="ILogger.Log" />.
        /// </summary>
        /// <param name="logLevel">The parameter is not used.</param>
        /// <param name="eventId">The parameter is not used.</param>
        /// <param name="state">The parameter is not used.</param>
        /// <param name="exception">The parameter is not used.</param>
        /// <param name="formatter">The parameter is not used.</param>
        /// <typeparam name="TState">The parameter is not used.</typeparam>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
        }

        private class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Value = new NullDisposable();

            private NullDisposable()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
