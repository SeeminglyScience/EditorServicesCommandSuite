using System.Collections.Generic;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.Language
{
    internal class UsingDescription
    {
        private static readonly Dictionary<UsingStatementKind, TokenKind> s_kindMap =
            new Dictionary<UsingStatementKind, TokenKind>()
            {
                { UsingStatementKind.Assembly, TokenKind.Assembly },
                { UsingStatementKind.Module, TokenKind.Module },
                { UsingStatementKind.Namespace, TokenKind.Namespace },
                { UsingStatementKind.Command, TokenKind.Command },
                { UsingStatementKind.Type, TokenKind.Type },
            };

        public string Text { get; set; }

        public UsingStatementKind Kind { get; set; }

        public override string ToString()
        {
            return TokenTraits.Text(TokenKind.Using)
                + Symbols.Space
                + TokenTraits.Text(s_kindMap[Kind])
                + Symbols.Space
                + Text;
        }
    }
}
