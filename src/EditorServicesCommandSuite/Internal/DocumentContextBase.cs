using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Represents the context of a refactor request.
    /// </summary>
    internal abstract class DocumentContextBase
    {
        internal readonly PSCmdlet _psCmdlet;

        internal DocumentContextBase(
            ScriptBlockAst rootAst,
            Ast currentAst,
            LinkedListNode<Token> currentToken,
            IScriptExtent selectionExtent,
            PSCmdlet psCmdlet,
            CancellationToken cancellationToken,
            ThreadController threadController)
        {
            RootAst = rootAst;
            Ast = currentAst;

            int parentCount = 0;
            for (var node = currentAst; node.Parent != null; node = node.Parent)
            {
                parentCount++;
            }

            var relatedAsts = ImmutableArray.CreateBuilder<Ast>(parentCount);
            for (var node = currentAst; node.Parent != null; node = node.Parent)
            {
                relatedAsts.Add(node);
            }

            RelatedAsts = relatedAsts.MoveToImmutable();
            Token = currentToken;
            SelectionExtent = selectionExtent;
            CancellationToken = cancellationToken;
            _psCmdlet = psCmdlet;
            PipelineThread = threadController;
        }

        /// <summary>
        /// Gets or sets the root <see cref="Ast" /> of the target document.
        /// </summary>
        internal virtual ScriptBlockAst RootAst { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Ast" /> that is the subject of the refactor request.
        /// </summary>
        /// <remarks>
        /// This is most commonly the <see cref="Ast" /> closest to the cursor location.
        /// </remarks>
        internal virtual Ast Ast { get; set; }

        internal virtual ImmutableArray<Ast> RelatedAsts { get; }

        /// <summary>
        /// Gets or sets a node in a <see cref="LinkedList{Token}" /> that is the
        /// subject of the refactor request.
        /// </summary>
        /// <remarks>
        /// This is most commonly the <see cref="Token" /> closest to the cursor location.
        /// </remarks>
        internal virtual LinkedListNode<Token> Token { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IScriptExtent" /> representing the current text
        /// selection. If there is no selection this be an extent with no range indicating
        /// the cursor position.
        /// </summary>
        internal virtual IScriptExtent SelectionExtent { get; set; }

        /// <summary>
        /// Gets or sets a <see cref="CancellationToken" /> that can be used to cancel
        /// the request.
        /// </summary>
        internal virtual CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// Gets or sets a tuple that indicates the start line, start column, end line,
        /// and end column respectively of the selected text.
        /// </summary>
        internal virtual Tuple<int, int, int, int> SelectionRange { get; set; }

        internal virtual ThreadController PipelineThread { get; set; }

        /// <summary>
        /// Retrieves the configuration that the refactor provider should use.
        /// </summary>
        /// <typeparam name="TConfiguration">The type of configuration.</typeparam>
        /// <returns>The configuration.</returns>
        internal abstract TConfiguration GetConfiguration<TConfiguration>()
            where TConfiguration : new();

        /// <summary>
        /// Attempts to retrieve the <see cref="PSCmdlet" /> for the request. If
        /// the request was created outside of PowerShell this will return
        /// <see langword="false" />.
        /// </summary>
        /// <param name="cmdlet">The cmdlet for the request.</param>
        /// <returns>
        /// "true" if there is a cmdlet attached to the request, otherwise "false".
        /// </returns>
        internal virtual bool TryGetCmdlet(out PSCmdlet cmdlet)
        {
            cmdlet = _psCmdlet;
            return _psCmdlet != null;
        }

        /// <summary>
        /// Sets the cursor position after all edits have been applied.
        /// </summary>
        /// <param name="position">The desired cursor position.</param>
        /// <returns>"true" if the session supports navigation, otherwise "false".</returns>
        internal virtual bool SetCursorPosition(IScriptPosition position)
        {
            return SetSelection(
                position.LineNumber,
                position.ColumnNumber,
                position.LineNumber,
                position.ColumnNumber);
        }

        /// <summary>
        /// Sets the cursor position after all edits have been applied.
        /// </summary>
        /// <param name="line">The desired line number.</param>
        /// <param name="column">The desired column number.</param>
        /// <returns>"true" if the session supports navigation, otherwise "false".</returns>
        internal virtual bool SetCursorPosition(int line, int column)
        {
            return SetSelection(
                line,
                column,
                line,
                column);
        }

        /// <summary>
        /// Sets the text selection after all edits have been applied.
        /// </summary>
        /// <param name="extent">The desired script extent.</param>
        /// <returns>"true" if the session supports setting selection, otherwise "false".</returns>
        internal virtual bool SetSelection(IScriptExtent extent)
        {
            return SetSelection(
                extent.StartLineNumber,
                extent.StartColumnNumber,
                extent.EndLineNumber,
                extent.EndColumnNumber);
        }

        /// <summary>
        /// Sets the text selection after all edits have been applied.
        /// </summary>
        /// <param name="startLine">The desired start line.</param>
        /// <param name="startColumn">The desired start column.</param>
        /// <param name="endLine">The desired end line.</param>
        /// <param name="endColumn">The desired end column.</param>
        /// <returns>"true" if the session supports setting selection, otherwise "false".</returns>
        internal virtual bool SetSelection(int startLine, int startColumn, int endLine, int endColumn)
        {
            if (SelectionRange != null)
            {
                return false;
            }

            SelectionRange = new Tuple<int, int, int, int>(
                startLine,
                startColumn,
                endLine,
                endColumn);

            return true;
        }
    }
}
