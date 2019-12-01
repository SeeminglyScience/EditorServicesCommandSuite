using System;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal partial class ChangeStringEnclosureRefactor
    {
        internal sealed class StringEnclosureInfo : IEquatable<StringEnclosureInfo>
        {
            public static readonly StringEnclosureInfo BareWord = new StringEnclosureInfo(
                StringEnclosureType.BareWord,
                string.Empty,
                string.Empty,
                "bare word expression");

            public static readonly StringEnclosureInfo Expandable = new StringEnclosureInfo(
                StringEnclosureType.Expandable,
                Symbols.DoubleQuote,
                Symbols.DoubleQuote,
                @"expandable - """"");

            public static readonly StringEnclosureInfo ExpandableHereString = new StringEnclosureInfo(
                StringEnclosureType.ExpandableHereString,
                Symbols.ExpandableHereStringOpen,
                Symbols.ExpandableHereStringClose,
                @"expandable here-string - @""""@");

            public static readonly StringEnclosureInfo Literal = new StringEnclosureInfo(
                StringEnclosureType.Literal,
                Symbols.SingleQuote,
                Symbols.SingleQuote,
                "literal - ''");

            public static readonly StringEnclosureInfo LiteralHereString = new StringEnclosureInfo(
                StringEnclosureType.LiteralHereString,
                Symbols.LiteralHereStringOpen,
                Symbols.LiteralHereStringClose,
                "literal here-string - @''@");

            private StringEnclosureInfo(
                StringEnclosureType type,
                char open,
                char close,
                string description)
                : this(type, open.ToString(), close.ToString(), description)
            {
            }

            private StringEnclosureInfo(
                StringEnclosureType type,
                char[] open,
                char[] close,
                string description)
                : this(type, new string(open), new string(close), description)
            {
            }

            private StringEnclosureInfo(
                StringEnclosureType type,
                string open,
                string close,
                string description)
            {
                Type = type;
                Open = open;
                Close = close;
                Description = description;
            }

            internal StringEnclosureType Type { get; }

            internal string Open { get; }

            internal string Close { get; }

            internal string Description { get; }

            public static bool operator ==(StringEnclosureInfo left, StringEnclosureInfo right)
            {
                if (left is null || right is null)
                {
                    return left is null && right is null;
                }

                return left.Equals(right);
            }

            public static bool operator !=(StringEnclosureInfo left, StringEnclosureInfo right)
            {
                if (left is null || right is null)
                {
                    return left is object || right is object;
                }

                return !left.Equals(right);
            }

            public override bool Equals(object obj) => obj is StringEnclosureInfo info && Equals(info);

            public override int GetHashCode() => Type.GetHashCode();

            public bool Equals(StringEnclosureInfo other) => Type == other.Type;
        }
    }
}
