using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal class NullDiagnosticService : IRefactorAnalysisContext
    {
        public IEnumerable<DiagnosticMarker> GetDiagnosticsFromContents(string contents, CancellationToken cancellationToken)
        {
            return Enumerable.Empty<DiagnosticMarker>();
        }

        public Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromContentsAsync(string contents, CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<DiagnosticMarker>());
        }

        public IEnumerable<DiagnosticMarker> GetDiagnosticsFromPath(string path, CancellationToken cancellationToken)
        {
            return Enumerable.Empty<DiagnosticMarker>();
        }

        public Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromPathAsync(string path, CancellationToken cancellationToken)
        {
            return Task.FromResult(Enumerable.Empty<DiagnosticMarker>());
        }
    }
}
