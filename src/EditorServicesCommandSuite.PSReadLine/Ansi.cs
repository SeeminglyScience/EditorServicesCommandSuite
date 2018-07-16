using Microsoft.PowerShell;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal static class Ansi
    {
        internal const string ClearScreen = "\x1b[2J";

        internal const string ClearCurrentLine = "\x1b[K";

        internal const string ClearLines = "\x1b[{0}M";

        internal const string EnterAlternateBuffer = "\x1b[?1049h";

        internal const string ExitAlternateBuffer = "\x1b[?1049l";

        private static readonly PSConsoleReadLineOptions s_psrlOptions = PSConsoleReadLine.GetOptions();

        internal static class Movement
        {
            internal const string SetVerticalCursorPosition = "\x1b[{0}d";

            internal const string SetHorizontalCursorPosition = "\x1b[{0}G";

            internal const string CursorRight = "\x1bC";

            internal const string CursorLeft = "\x1bD";
        }

        internal static class Colors
        {
            internal const string Reset = "\x1b[0m\x1b[24m\x1b[27m";

            internal static string Primary => Default;

            internal static string Secondary => Operator;

            internal static string Success { get; } = "\x1b[38;2;0;126;51";

            internal static string Warning { get; } = "\x1b[38;2;255;136;0";

            internal static string Danger { get; } = "\x1b[38;2;204;0;0";

            internal static string Information { get; } = "\x1b[38;2;0;153;204";

            internal static string Command { get; } = (string)s_psrlOptions.CommandColor;

            internal static string Comment { get; } = (string)s_psrlOptions.CommentColor;

            internal static string ContinuationPrompt { get; } = (string)s_psrlOptions.ContinuationPromptColor;

            internal static string Default { get; } = (string)s_psrlOptions.DefaultTokenColor;

            internal static string Emphasis { get; } = (string)s_psrlOptions.EmphasisColor;

            internal static string Error { get; } = (string)s_psrlOptions.ErrorColor;

            internal static string Keyword { get; } = (string)s_psrlOptions.KeywordColor;

            internal static string Member { get; } = (string)s_psrlOptions.MemberColor;

            internal static string Number { get; } = (string)s_psrlOptions.NumberColor;

            internal static string Operator { get; } = (string)s_psrlOptions.OperatorColor;

            internal static string Parameter { get; } = (string)s_psrlOptions.ParameterColor;

            internal static string Selection { get; } = (string)s_psrlOptions.SelectionColor;

            internal static string String { get; } = (string)s_psrlOptions.StringColor;

            internal static string Type { get; } = (string)s_psrlOptions.TypeColor;

            internal static string Variable { get; } = (string)s_psrlOptions.VariableColor;
        }
    }
}
