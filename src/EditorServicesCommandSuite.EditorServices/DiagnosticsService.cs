using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices.Utility;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class DiagnosticsService : IRefactorAnalysisContext
    {
        private static readonly DiagnosticMarker[] s_emptyMarkers = new DiagnosticMarker[0];

        private readonly IPowerShellExecutor _executor;

        public DiagnosticsService(IPowerShellExecutor executor)
        {
            Validate.IsNotNull(nameof(executor), executor);
            _executor = executor;
        }

        public IEnumerable<DiagnosticMarker> GetDiagnosticsFromPath(
            string path,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return s_emptyMarkers;
            }

            if (!File.Exists(Path.GetFullPath(path)))
            {
                return s_emptyMarkers;
            }

            return GetDiagnosticsImpl("Path", path, cancellationToken);
        }

        public IEnumerable<DiagnosticMarker> GetDiagnosticsFromContents(
            string contents,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(contents))
            {
                return s_emptyMarkers;
            }

            return GetDiagnosticsImpl("ScriptDefinition", contents, cancellationToken);
        }

        public async Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromPathAsync(
            string path,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return s_emptyMarkers;
            }

            if (!File.Exists(Path.GetFullPath(path)))
            {
                return s_emptyMarkers;
            }

            return await GetDiagnosticsImplAsync("Path", path, cancellationToken);
        }

        public async Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromContentsAsync(
            string contents,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(contents))
            {
                return s_emptyMarkers;
            }

            return await GetDiagnosticsImplAsync("ScriptDefinition", contents, cancellationToken);
        }

        private static DiagnosticMarker ToDiagnosticMarker(PSObject marker)
        {
            return (DiagnosticMarker)LanguagePrimitives.ConvertPSObjectToType(
                marker,
                typeof(DiagnosticMarker),
                recursion: true,
                CultureInfo.InvariantCulture,
                ignoreUnknownMembers: true);
        }

        private IEnumerable<DiagnosticMarker> GetDiagnosticsImpl(
            string pssaParameterName,
            string value,
            CancellationToken cancellationToken)
        {
            return _executor.ExecuteCommand<PSObject>(
                new PSCommand()
                    .AddCommand("Invoke-ScriptAnalyzer")
                    .AddParameter(pssaParameterName, value)
                    .AddCommand("Select-Object")
                    .AddParameter("Property", "*"),
                cancellationToken)
                .Select(ToDiagnosticMarker);
        }

        private async Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsImplAsync(
            string pssaParameterName,
            string value,
            CancellationToken cancellationToken)
        {
            return (await _executor.ExecuteCommandAsync<PSObject>(
                new PSCommand()
                    .AddCommand("Invoke-ScriptAnalyzer")
                    .AddParameter(pssaParameterName, value)
                    .AddCommand("Select-Object")
                    .AddParameter("Property", "*"),
                cancellationToken))
                .Select(ToDiagnosticMarker);
        }
    }
}
