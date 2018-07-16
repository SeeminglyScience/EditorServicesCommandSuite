using System.Management.Automation.Language;
using System.Text;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal static class Util
    {
        public static int GetStringHeight(string content, int maxLineLength)
        {
            if (string.IsNullOrEmpty(content))
            {
                return 1;
            }

            int contentLines = 1;
            int column = 0;
            for (var i = 0; i < content.Length; i++, column++)
            {
                if (column >= maxLineLength || content[i] == '\n')
                {
                    contentLines++;
                    column = -1;
                    continue;
                }

                if (content[i] == '\r')
                {
                    column--;
                }
            }

            return contentLines;
        }

        public static string GetRenderedScript(Token[] tokens)
        {
            var sb = new StringBuilder();
            int lastEndOffset = 0;
            for (var i = 0; i < tokens.Length; i++)
            {
                sb.Append(
                    Symbols.Space,
                    tokens[i].Extent.StartOffset - lastEndOffset);

                lastEndOffset = tokens[i].Extent.EndOffset;
                switch (tokens[i])
                {
                    case StringToken stringToken:
                    {
                        if (stringToken.TokenFlags.HasFlag(TokenFlags.CommandName))
                        {
                            sb.Append(Ansi.Colors.Command);
                            break;
                        }

                        sb.Append(Ansi.Colors.String);
                        break;
                    }
                    case NumberToken numberToken: sb.Append(Ansi.Colors.Number); break;
                    case ParameterToken parameterToken: sb.Append(Ansi.Colors.Parameter); break;
                    case VariableToken variableToken: sb.Append(Ansi.Colors.Variable); break;
                    default:
                    {
                        if (tokens[i].TokenFlags.HasFlag(TokenFlags.BinaryOperator) ||
                            tokens[i].TokenFlags.HasFlag(TokenFlags.UnaryOperator))
                        {
                            sb.Append(Ansi.Colors.Operator);
                            break;
                        }

                        if (tokens[i].TokenFlags.HasFlag(TokenFlags.CommandName))
                        {
                            sb.Append(Ansi.Colors.Command);
                            break;
                        }

                        if (tokens[i].TokenFlags.HasFlag(TokenFlags.MemberName))
                        {
                            sb.Append(Ansi.Colors.Member);
                            break;
                        }

                        if (tokens[i].TokenFlags.HasFlag(TokenFlags.TypeName))
                        {
                            sb.Append(Ansi.Colors.Type);
                            break;
                        }

                        if (tokens[i].TokenFlags.HasFlag(TokenFlags.Keyword))
                        {
                            sb.Append(Ansi.Colors.Keyword);
                            break;
                        }

                        sb.Append(Ansi.Colors.Default);
                        break;
                    }
                }

                sb.Append(tokens[i].Text);
            }

            sb.Append(Ansi.Colors.Reset);
            return sb.ToString();
        }
    }
}
