using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal class NullDiagnosticService : IRefactorAnalysisContext
    {
        public Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromPathAsync(
            string path,
            ThreadController pipelineThread,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<DiagnosticMarker>());
        }

        public Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromContentsAsync(
            string contents,
            ThreadController pipelineThread,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<DiagnosticMarker>());
        }
    }
}
