using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides the ability to process <see cref="DocumentEdit" /> objects.
    /// </summary>
    internal interface IDocumentEditProcessor
    {
        /// <summary>
        /// Apply document edits to the current document.
        /// </summary>
        /// <param name="edits">The edits to apply.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        Task WriteDocumentEditsAsync(IEnumerable<DocumentEdit> edits, CancellationToken cancellationToken);
    }
}
