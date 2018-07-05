using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal class EmptyPosition : IScriptPosition
    {
        public int ColumnNumber => 0;

        public string File => string.Empty;

        public string Line => string.Empty;

        public int LineNumber => 0;

        public int Offset => 0;

        public string GetFullScript() => string.Empty;

        internal static EmptyPosition Empty => new EmptyPosition();

    }
}
