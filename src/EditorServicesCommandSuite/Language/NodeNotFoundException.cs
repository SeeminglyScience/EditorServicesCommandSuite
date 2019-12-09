using System;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    /// <summary>
    /// The exception that is thrown when an expected node (e.g. a <see cref="Token" />
    /// or <see cref="Ast" />) is not found.
    /// </summary>
    public class NodeNotFoundException : InvalidOperationException, IContainsErrorRecord
    {
        private readonly string _errorId;

        private readonly object _target;

        private ErrorRecord _errorRecord;

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeNotFoundException" /> class.
        /// </summary>
        public NodeNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeNotFoundException" /> class
        /// with the specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public NodeNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NodeNotFoundException" /> class
        /// with a specified error message and a reference to the inner exception that
        /// is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">
        /// The exception that is the cause of the current exception. If the
        /// <see paramref="innerException" /> parameter is not a null reference,
        /// the current exception is raised in a catch block that handles the
        /// inner exception.
        /// </param>
        public NodeNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        internal NodeNotFoundException(
            string message,
            Exception innerException,
            string errorId,
            object target)
            : base(message, innerException)
        {
            _errorId = errorId;
            _target = target;
        }

        /// <summary>
        /// Gets the <see cref="ErrorRecord" /> which provides additional information
        /// about the error.
        /// </summary>
        public ErrorRecord ErrorRecord => _errorRecord ??= new ErrorRecord(
            new ParentContainsErrorRecordException(Message),
            _errorId ?? "NodeNotFound",
            ErrorCategory.ObjectNotFound,
            _target);
    }
}
