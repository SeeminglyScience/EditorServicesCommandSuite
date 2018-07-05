using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class DocumentContext : DocumentContextBase
    {
        internal DocumentContext(
            ScriptBlockAst rootAst,
            Ast currentAst,
            LinkedListNode<Token> currentToken,
            IScriptExtent selectionExtent)
            : base(rootAst, currentAst, currentToken, selectionExtent, null, CancellationToken.None)
        {
        }

        internal DocumentContext(
            ScriptBlockAst rootAst,
            Ast currentAst,
            LinkedListNode<Token> currentToken,
            IScriptExtent selectionExtent,
            CancellationToken cancellationToken)
            : base(rootAst, currentAst, currentToken, selectionExtent, null, cancellationToken)
        {
        }

        internal DocumentContext(
            ScriptBlockAst rootAst,
            Ast currentAst,
            LinkedListNode<Token> currentToken,
            IScriptExtent selectionExtent,
            PSCmdlet cmdlet)
            : base(rootAst, currentAst, currentToken, selectionExtent, cmdlet, CancellationToken.None)
        {
        }

        internal DocumentContext(
            ScriptBlockAst rootAst,
            Ast currentAst,
            LinkedListNode<Token> currentToken,
            IScriptExtent selectionExtent,
            PSCmdlet cmdlet,
            CancellationToken cancellationToken)
            : base(rootAst, currentAst, currentToken, selectionExtent, cmdlet, cancellationToken)
        {
        }

        internal override TConfiguration GetConfiguration<TConfiguration>()
        {
            return new TConfiguration();
        }
    }
}
