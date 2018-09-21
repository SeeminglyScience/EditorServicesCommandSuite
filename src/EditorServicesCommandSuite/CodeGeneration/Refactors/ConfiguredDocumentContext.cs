using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class ConfiguredDocumentContext<TConfiguration>
        : DocumentContextBase
        where TConfiguration : class
    {
        internal ConfiguredDocumentContext(
            TConfiguration configuration,
            DocumentContextBase contextToCopy)
            : this(
                configuration,
                contextToCopy.RootAst,
                contextToCopy.Ast,
                contextToCopy.Token,
                contextToCopy.SelectionExtent,
                contextToCopy._psCmdlet,
                contextToCopy.CancellationToken,
                contextToCopy.PipelineThread)
        {
        }

        internal ConfiguredDocumentContext(
            TConfiguration configuration,
            DocumentContextBase contextToCopy,
            CancellationToken cancellationToken)
            : this(
                configuration,
                contextToCopy.RootAst,
                contextToCopy.Ast,
                contextToCopy.Token,
                contextToCopy.SelectionExtent,
                contextToCopy._psCmdlet,
                cancellationToken,
                contextToCopy.PipelineThread)
        {
        }

        internal ConfiguredDocumentContext(
            TConfiguration configuration,
            ScriptBlockAst rootAst,
            Ast currentAst,
            LinkedListNode<Token> currentToken,
            IScriptExtent selectionExtent,
            PSCmdlet cmdlet,
            CancellationToken cancellationToken,
            ThreadController threadController)
            : base(
                rootAst,
                currentAst,
                currentToken,
                selectionExtent,
                cmdlet,
                cancellationToken,
                threadController)
        {
            Configuration = configuration;
        }

        internal TConfiguration Configuration { get; }

        internal override TConfiguration1 GetConfiguration<TConfiguration1>()
        {
            if (Configuration is TConfiguration1 targetConfiguration)
            {
                return targetConfiguration;
            }

            return new TConfiguration1();
        }
    }
}
