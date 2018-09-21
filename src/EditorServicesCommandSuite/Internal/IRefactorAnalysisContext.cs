using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides the ability to retrieve diagnostic markers for the current document.
    /// </summary>
    internal interface IRefactorAnalysisContext
    {
        /// <summary>
        /// Gets diagnostic markers for a specific document.
        /// </summary>
        /// <param name="path">The path of the document.</param>
        /// <param name="pipelineThread">
        /// The controller for the PowerShell pipeline thread.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain active diagnostic markers.
        /// </returns>
        Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromPathAsync(
            string path,
            ThreadController pipelineThread,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets diagnostic markers for the contents of an untitled document.
        /// </summary>
        /// <param name="contents">The text of the document to analyze.</param>
        /// <param name="pipelineThread">
        /// The controller for the PowerShell pipeline thread.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain active diagnostic markers.
        /// </returns>
        Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromContentsAsync(
            string contents,
            ThreadController pipelineThread,
            CancellationToken cancellationToken);
    }
}
