using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal class EmptyExtent : IScriptExtent
    {
        public int EndColumnNumber => 0;

        public int EndLineNumber => 0;

        public int EndOffset => 0;

        public IScriptPosition EndScriptPosition => EmptyPosition.Empty;

        public string File => string.Empty;

        public int StartColumnNumber => 0;

        public int StartLineNumber => 0;

        public int StartOffset => 0;

        public IScriptPosition StartScriptPosition => EmptyPosition.Empty;

        public string Text => string.Empty;

        internal static EmptyExtent Empty => new EmptyExtent();
    }
}
