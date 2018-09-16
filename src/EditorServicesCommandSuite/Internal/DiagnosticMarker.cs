using System.ComponentModel;
using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Represents a diagnostic marker created by an analyzer.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class DiagnosticMarker
    {
        /// <summary>
        /// Gets or sets the script extent flagged by the marker.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IScriptExtent Extent { get; set; }

        /// <summary>
        /// Gets or sets the name of the rule violated.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string RuleName { get; set; }

        /// <summary>
        /// Gets or sets the ID of the rule violated.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string RuleSuppressionId { get; set; }

        /// <summary>
        /// Gets or sets the name of the script where this marker is present.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string ScriptName { get; set; }

        /// <summary>
        /// Gets or sets the path of the script where this marker is present.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string ScriptPath { get; set; }

        /// <summary>
        /// Gets or sets the severity of the violated rule.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string Severity { get; set; }
    }
}
