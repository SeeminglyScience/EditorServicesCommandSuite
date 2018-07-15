namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides constants and static instances of common symbols.
    /// </summary>
    public static class Symbols
    {
        /// <summary>
        /// The character: "&gt;"
        /// </summary>
        public const char GreaterThan = '>';

        /// <summary>
        /// The character: "&lt;"
        /// </summary>
        public const char LessThan = '<';

        /// <summary>
        /// The character: "#"
        /// </summary>
        public const char NumberSign = '#';

        /// <summary>
        /// The character: "\"
        /// </summary>
        public const char Backslash = '\\';

        /// <summary>
        /// The character: "/"
        /// </summary>
        public const char ForwardSlash = '/';

        /// <summary>
        /// The character: ":"
        /// </summary>
        public const char Colon = ':';

        /// <summary>
        /// The character: ","
        /// </summary>
        public const char Comma = ',';

        /// <summary>
        /// The character: "-"
        /// </summary>
        public const char Dash = '-';

        /// <summary>
        /// The character: "["
        /// </summary>
        public const char SquareOpen = '[';

        /// <summary>
        /// The character: "]"
        /// </summary>
        public const char SquareClose = ']';

        /// <summary>
        /// The character: "{"
        /// </summary>
        public const char CurlyOpen = '{';

        /// <summary>
        /// The character: "}"
        /// </summary>
        public const char CurlyClose = '}';

        /// <summary>
        /// The character: "("
        /// </summary>
        public const char ParenOpen = '(';

        /// <summary>
        /// The character: ")"
        /// </summary>
        public const char ParenClose = ')';

        /// <summary>
        /// The character: "$"
        /// </summary>
        public const char Dollar = '$';

        /// <summary>
        /// The character: "`"
        /// </summary>
        public const char Backtick = '`';

        /// <summary>
        /// The character: "."
        /// </summary>
        public const char Dot = '.';

        /// <summary>
        /// The character: " "
        /// </summary>
        public const char Space = ' ';

        /// <summary>
        /// The character: ";"
        /// </summary>
        public const char Semi = ';';

        /// <summary>
        /// The character: "'"
        /// </summary>
        public const char SingleQuote = '\'';

        /// <summary>
        /// The character: "
        /// </summary>
        public const char DoubleQuote = '"';

        /// <summary>
        /// The character: "@"
        /// </summary>
        public const char At = '@';

        /// <summary>
        /// The character: "="
        /// </summary>
        public const char Equal = '=';

        /// <summary>
        /// The character: "_"
        /// </summary>
        public const char Underscore = '_';

        /// <summary>
        /// The character: "}"
        /// </summary>
        public const char HashtableClose = CurlyClose;

        /// <summary>
        /// An array of characters for the string "$null"
        /// </summary>
        public static readonly char[] Null = { Dollar, 'n', 'u', 'l', 'l' };

        /// <summary>
        /// An array of characters for the string "new"
        /// </summary>
        public static readonly char[] New = { 'n', 'e', 'w' };

        /// <summary>
        /// An array of characters for the string "param"
        /// </summary>
        public static readonly char[] Param = { 'p', 'a', 'r', 'a', 'm' };

        /// <summary>
        /// An array of characters for the string "static"
        /// </summary>
        public static readonly char[] Static = { 's', 't', 'a', 't', 'i', 'c' };

        /// <summary>
        /// An array of characters for the string "throw"
        /// </summary>
        public static readonly char[] Throw = { 't', 'h', 'r', 'o', 'w' };

        /// <summary>
        /// An array of characters for the string "using"
        /// </summary>
        public static readonly char[] Using = { 'u', 's', 'i', 'n', 'g' };

        /// <summary>
        /// An array of characters for the string "namespace"
        /// </summary>
        public static readonly char[] Namespace = { 'n', 'a', 'm', 'e', 's', 'p', 'a', 'c', 'e' };

        /// <summary>
        /// An array of characters for the string "assembly"
        /// </summary>
        public static readonly char[] Assembly = { 'a', 's', 's', 'e', 'm', 'b', 'l', 'y' };

        /// <summary>
        /// An array of characters for the string "module"
        /// </summary>
        public static readonly char[] Module = { 'm', 'o', 'd', 'u', 'l', 'e' };

        /// <summary>
        /// An array of characters for the string "Attribute"
        /// </summary>
        public static readonly char[] Attribute = { 'A', 't', 't', 'r', 'i', 'b', 'u', 't', 'e' };

        /// <summary>
        /// An array of characters for the string "::"
        /// </summary>
        public static readonly char[] StaticOperator = { Colon, Colon };

        /// <summary>
        /// An array of characters for the string @"
        /// </summary>
        public static readonly char[] ExpandableHereStringOpen = { At, DoubleQuote };

        /// <summary>
        /// An array of characters for the string "@
        /// </summary>
        public static readonly char[] ExpandableHereStringClose = { DoubleQuote, At };

        /// <summary>
        /// An array of characters for the string "@'"
        /// </summary>
        public static readonly char[] LiteralHereStringOpen = { At, SingleQuote };

        /// <summary>
        /// An array of characters for the string "'@"
        /// </summary>
        public static readonly char[] LiteralHereStringClose = { SingleQuote, At };

        /// <summary>
        /// An array of characters for the string " = "
        /// </summary>
        public static readonly char[] SpaceEnclosedEqual = { Space, Equal, Space };

        /// <summary>
        /// An array of characters for the string " - "
        /// </summary>
        public static readonly char[] SpaceEnclosedDash = { Space, Dash, Space };

        /// <summary>
        /// An array of characters for the string "@{"
        /// </summary>
        public static readonly char[] HashtableOpen = { At, CurlyOpen };

        /// <summary>
        /// An array of characters for the string ", "
        /// </summary>
        public static readonly char[] GenericTypeArgumentSeparator = { Comma, Space };

        /// <summary>
        /// An array of characters for the string ", "
        /// </summary>
        public static readonly char[] MethodParameterSeparator = { Comma, Space };

        /// <summary>
        /// An array of characters for the string "[Environment]::NewLine"
        /// </summary>
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
