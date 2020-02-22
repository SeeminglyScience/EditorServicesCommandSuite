using Microsoft.PowerShell.EditorServices.Extensions;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class LspPosition : ILspFilePosition
    {
        public LspPosition(long line, long character)
        {
            Line = line;
            Character = character;
        }

        public long Line { get; }

        public long Character { get; }
    }
}
