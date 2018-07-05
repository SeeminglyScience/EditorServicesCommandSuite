using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    public interface IRefactorAnalysisContext
    {
        IEnumerable<DiagnosticMarker> GetDiagnosticsFromPath(string path, CancellationToken cancellationToken);

        IEnumerable<DiagnosticMarker> GetDiagnosticsFromContents(string contents, CancellationToken cancellationToken);

        Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromPathAsync(string path, CancellationToken cancellationToken);

        Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromContentsAsync(string contents, CancellationToken cancellationToken);
    }
}
