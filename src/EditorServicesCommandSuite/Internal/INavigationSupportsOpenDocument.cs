using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides the ability to open documents in the host editor.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface INavigationSupportsOpenDocument
    {
        /// <summary>
        /// Opens a document in the host editor.
        /// </summary>
        /// <param name="path">The path of the document to open.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void OpenDocument(string path);

        /// <summary>
        /// Opens a document in the host editor.
        /// </summary>
        /// <param name="path">The path of the document to open.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void OpenDocument(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Opens a document in the host editor.
        /// </summary>
        /// <param name="path">The path of the document to open.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task OpenDocumentAsync(string path);

        /// <summary>
        /// Opens a document in the host editor.
        /// </summary>
        /// <param name="path">The path of the document to open.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task OpenDocumentAsync(string path, CancellationToken cancellationToken);
    }
}
