using System.Management.Automation;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class SurroundSelectedLinesSettings : RefactorConfiguration
    {
        [Parameter(Position = 0)]
        [ValidateNotNull]
        public ExpressionSurroundType? SurroundType { get; set; }
    }
}
