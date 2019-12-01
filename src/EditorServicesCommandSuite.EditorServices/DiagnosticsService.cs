using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class DiagnosticsService : IRefactorAnalysisContext
    {
        private static readonly DiagnosticMarker[] s_emptyMarkers = new DiagnosticMarker[0];

        public async Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromPathAsync(
            string path,
            ThreadController pipelineThread,
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

            return await GetDiagnosticsImplAsync(
                "Path",
                path,
                pipelineThread,
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsFromContentsAsync(
            string contents,
            ThreadController pipelineThread,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(contents))
            {
                return s_emptyMarkers;
            }

            return await GetDiagnosticsImplAsync(
                "ScriptDefinition",
                contents,
                pipelineThread,
                cancellationToken)
                .ConfigureAwait(false);
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

        private async Task<IEnumerable<DiagnosticMarker>> GetDiagnosticsImplAsync(
            string pssaParameterName,
            string value,
            ThreadController pipelineThread,
            CancellationToken cancellationToken)
        {
            return await pipelineThread.InvokeAsync(
                () =>
                {
                    using (var pwsh = PowerShell.Create(RunspaceMode.CurrentRunspace))
                    using (cancellationToken.Register(() => pwsh.BeginStop(null, null)))
                    {
                        return
                            pwsh.AddCommand("PSScriptAnalyzer\\Invoke-ScriptAnalyzer")
                                .AddParameter(pssaParameterName, value)
                                .AddCommand("Microsoft.PowerShell.Utility\\Select-Object")
                                .AddParameter("Property", "*")
                                .Invoke<PSObject>()
                                .Select(ToDiagnosticMarker);
                    }
                }).ConfigureAwait(false);
        }
    }
}
