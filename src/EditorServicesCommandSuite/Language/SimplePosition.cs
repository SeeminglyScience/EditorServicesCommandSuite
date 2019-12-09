using System;
using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal readonly struct SimplePosition : IEquatable<SimplePosition>
    {
        public readonly int Offset;

        public SimplePosition(int offset) => Offset = offset;

        public SimplePosition(IScriptPosition position) => Offset = position.Offset;

        public static implicit operator SimplePosition(int offset) => new SimplePosition(offset);

        public static implicit operator int(SimplePosition position) => position.Offset;

        public bool Equals(SimplePosition other) => other.Offset == Offset;

        public override bool Equals(object obj) => obj is SimplePosition position && Equals(position);

        public override int GetHashCode() => Offset.GetHashCode();

        public override string ToString() => string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            LanguageStrings.SimplePositionFormat,
            Offset);
    }
}
