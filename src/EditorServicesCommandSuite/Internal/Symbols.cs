namespace EditorServicesCommandSuite.Internal
{
    public static class Symbols
    {
        public const char Backslash = '\\';

        public const char ForwardSlash = '/';

        public const char Colon = ':';

        public const char Comma = ',';

        public const char Dash = '-';

        public const char SquareOpen = '[';

        public const char SquareClose = ']';

        public const char CurlyOpen = '{';

        public const char CurlyClose = '}';

        public const char ParenOpen = '(';

        public const char ParenClose = ')';

        public const char Dollar = '$';

        public const char Backtick = '`';

        public const char Dot = '.';

        public const char Space = ' ';

        public const char Semi = ';';

        public const char SingleQuote = '\'';

        public const char DoubleQuote = '"';

        public const char At = '@';

        public const char Equal = '=';

        public const char TypeLiteralBegin = SquareOpen;

        public const char TypeLiteralEnd = SquareClose;

        public const char ScriptBlockClose = CurlyClose;

        public const char ScriptBlockOpen = CurlyOpen;

        public const char HashtableClose = CurlyClose;

        public const char VariableOpen = Dollar;

        public const char PropertyClose = Semi;

        public const char ParametersOpen = ParenOpen;

        public const char ParametersClose = ParenClose;

        public static readonly char[] New = { 'n', 'e', 'w' };

        public static readonly char[] Param = { 'p', 'a', 'r', 'a', 'm' };

        public static readonly char[] Static = { 's', 't', 'a', 't', 'i', 'c' };

        public static readonly char[] Throw = { 't', 'h', 'r', 'o', 'w' };

        public static readonly char[] Using = { 'u', 's', 'i', 'n', 'g' };

        public static readonly char[] Namespace = { 'n', 'a', 'm', 'e', 's', 'p', 'a', 'c', 'e' };

        public static readonly char[] Assembly = { 'a', 's', 's', 'e', 'm', 'b', 'l', 'y' };

        public static readonly char[] Module = { 'm', 'o', 'd', 'u', 'l', 'e' };

        public static readonly char[] Attribute = { 'A', 't', 't', 'r', 'i', 'b', 'u', 't', 'e' };

        public static readonly char[] StaticOperator = { Colon, Colon };

        public static readonly char[] ExpandableHereStringOpen = { At, DoubleQuote };

        public static readonly char[] ExpandableHereStringClose = { DoubleQuote, At };

        public static readonly char[] LiteralHereStringOpen = { At, SingleQuote };

        public static readonly char[] LiteralHereStringClose = { SingleQuote, At };

        public static readonly char[] SpaceEnclosedEqual = { Space, Equal, Space };

        public static readonly char[] SpaceEnclosedDash = { Space, Dash, Space };

        public static readonly char[] HashtableOpen = { At, CurlyOpen };

        public static readonly char[] GenericTypeArgumentSeparator = { Comma, Space };

        public static readonly char[] MethodParameterSeparator = { Comma, Space };

        public static readonly char[] EnvironmentNewLine =
        {
            SquareOpen,
            'E', 'n', 'v', 'i', 'r', 'o', 'n', 'm', 'e', 'n', 't',
            SquareClose,
            Colon, Colon,
            'N', 'e', 'w', 'L', 'i', 'n', 'e',
        };
    }
}
