using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class DocumentContext : DocumentContextBase
    {
        internal DocumentContext(
            ScriptBlockAst rootAst,
            Ast currentAst,
            TokenNode currentToken,
            IScriptExtent selectionExtent,
            PSCmdlet cmdlet,
            CancellationToken cancellationToken,
            ThreadController threadController)
            : base(rootAst, currentAst, currentToken, selectionExtent, cmdlet, cancellationToken, threadController)
        {
        }

        internal override TConfiguration GetConfiguration<TConfiguration>()
        {
            return new TConfiguration();
        }
    }
}
