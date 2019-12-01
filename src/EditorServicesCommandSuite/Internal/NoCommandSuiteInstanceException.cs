using System;
using System.Management.Automation;
using System.Runtime.Serialization;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// The exception thrown when an attempt is made to interact with CommandSuite before it has
    /// been initialized.
    /// </summary>
    public class NoCommandSuiteInstanceException : Exception, IContainsErrorRecord
    {
        private ErrorRecord _error;

        internal NoCommandSuiteInstanceException()
            : base(RefactorStrings.NoCommandSuiteInstance)
        {
        }

        internal NoCommandSuiteInstanceException(string message)
            : base(message)
        {
        }

        internal NoCommandSuiteInstanceException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoCommandSuiteInstanceException" /> class.
        /// </summary>
        /// <param name="info">The serialization information.</param>
        /// <param name="context">The streaming context.</param>
        protected NoCommandSuiteInstanceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        /// Gets the <see cref="ErrorRecord" /> generated for the exception.
        /// </summary>
        public ErrorRecord ErrorRecord
        {
            get
            {
                return _error ?? (_error = new ErrorRecord(
                    this,
                    nameof(RefactorStrings.NoCommandSuiteInstance),
                    ErrorCategory.ObjectNotFound,
                    null));
            }
        }
    }
}
