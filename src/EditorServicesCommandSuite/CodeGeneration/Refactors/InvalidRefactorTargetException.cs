using System;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    /// <summary>
    /// The exception that is thrown when a refactor provider is invoked with the
    /// wrong target.
    /// </summary>
    public sealed class InvalidRefactorTargetException : InvalidOperationException
    {
        internal InvalidRefactorTargetException(DocumentContextBase context)
            : base(RefactorStrings.InvalidRefactorTarget)
        {
            RefactorContext = context;
        }

        internal InvalidRefactorTargetException(DocumentContextBase context, string message)
            : base(message)
        {
            RefactorContext = context;
        }

        internal InvalidRefactorTargetException(DocumentContextBase context, string message, Exception innerException)
            : base(message, innerException)
        {
            RefactorContext = context;
        }

        internal DocumentContextBase RefactorContext { get; }
    }
}
