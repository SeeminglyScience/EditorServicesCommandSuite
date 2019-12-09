using System;
using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal readonly struct SimpleRange : IEquatable<SimpleRange>
    {
        public readonly int Start;

        public readonly int End;

        public SimpleRange(int start, int end)
        {
            Start = start;
            End = end;
        }

        public SimpleRange(IScriptExtent extent)
        {
            if (extent == null)
            {
                Start = 0;
                End = 0;
                return;
            }

            Start = extent.StartOffset;
            End = extent.EndOffset;
        }

        public static implicit operator SimpleRange(Ast ast)
            => new SimpleRange(ast.Extent);

        public static implicit operator SimpleRange(Token token)
            => new SimpleRange(token.Extent);

        public static implicit operator SimpleRange(TokenNode node)
            => new SimpleRange(node.Value?.Extent);

        public static implicit operator SimpleRange((int start, int end) source)
            => new SimpleRange(source.start, source.end);

        public static bool operator ==(SimpleRange left, SimpleRange right) => left.Equals(right);

        public static bool operator !=(SimpleRange left, SimpleRange right) => !left.Equals(right);

        public bool Equals(SimpleRange other) => Start == other.Start && End == other.End;

        public override bool Equals(object obj) => obj is SimpleRange other && Equals(other);

        public override int GetHashCode()
        {
            int hash = 17;
            hash = (hash * 31) + Start.GetHashCode();
            hash = (hash * 31) + End.GetHashCode();
            return hash;
        }

        public void Deconstruct(out int startOffset, out int endOffset)
        {
            startOffset = Start;
            endOffset = End;
        }

        public override string ToString() => string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            LanguageStrings.SimpleRangeFormat,
            Start,
            End);
    }
}
