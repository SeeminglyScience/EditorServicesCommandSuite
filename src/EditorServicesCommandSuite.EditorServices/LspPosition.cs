using Microsoft.PowerShell.EditorServices.Extensions;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class LspPosition : ILspFilePosition
    {
        public LspPosition(int line, int character)
        {
            Line = line;
            Character = character;
        }

        public int Line { get; }

        public int Character { get; }
    }
}
