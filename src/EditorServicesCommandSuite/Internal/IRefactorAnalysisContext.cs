using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides the ability to retrieve diagnostic markers for the current document.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRefactorAnalysisContext
    {
        /// <summary>
        /// Gets diagnostic markers for a specific document.
        /// </summary>
        /// <param name="path">The path of the document.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>The active diagnostic markers.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        IEnumerable<DiagnosticMarker> GetDiagnosticsFromPath(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Gets diagnostic markers for the contents of an untitled document.
        /// </summary>
        /// <param name="contents">The text of the document to analyze.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>The active diagnostic markers.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        IEnumerable<DiagnosticMarker> GetDiagnosticsFromContents(string contents, CancellationToken cancellationToken);

        /// <summary>
        /// Gets diagnostic markers for a specific document.
        /// </summary>
        /// <param name="path">The path of the document.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain active diagnostic markers.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromPathAsync(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Gets diagnostic markers for the contents of an untitled document.
        /// </summary>
        /// <param name="contents">The text of the document to analyze.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain active diagnostic markers.
        /// </returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromContentsAsync(string contents, CancellationToken cancellationToken);
    }
}
