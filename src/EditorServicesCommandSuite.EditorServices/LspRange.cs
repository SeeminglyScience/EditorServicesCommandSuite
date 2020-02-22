using System.ComponentModel;
using Microsoft.PowerShell.EditorServices.Extensions;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class LspRange : ILspFileRange
    {
        public LspRange(ILspFilePosition start, ILspFilePosition end)
        {
            Start = start;
            End = end;
        }

        public ILspFilePosition Start { get; }

        public ILspFilePosition End { get; }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void Deconstruct(out ILspFilePosition start, out ILspFilePosition end)
        {
            start = Start;
            end = End;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void Deconstruct(
            out long startLine,
            out long startCharacter,
            out long endLine,
            out long endCharacter)
        {
            startLine = Start.Line;
            startCharacter = Start.Character;
            endLine = End.Line;
            endCharacter = End.Character;
        }
    }
}
