using System;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides context for the current state of editor host.
    /// </summary>
    internal abstract class DocumentContextProvider
    {
        /// <summary>
        /// Gets the path to the current workspace.
        /// </summary>
        internal abstract string Workspace { get; }

        /// <summary>
        /// Gets the context of the current state of the editor host.
        /// </summary>
        /// <param name="cmdlet">The <see cref="PSCmdlet" /> requesting the context.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <param name="pipelineThread">
        /// The controller for the PowerShell pipeline thread.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain the requested context.
        /// </returns>
        internal abstract Task<DocumentContextBase> GetDocumentContextAsync(
            PSCmdlet cmdlet,
            CancellationToken cancellationToken,
            ThreadController pipelineThread);

        /// <summary>
        /// Creates a helper object that can be used to easily build context.
        /// </summary>
        /// <param name="scriptText">The full text of the current document.</param>
        /// <returns>The helper object.</returns>
        private protected DocumentContextBuilder GetContextBuilder(string scriptText)
        {
            return new DocumentContextBuilder(scriptText);
        }

        /// <summary>
        /// Creates a helper object that can be used to easily build context.
        /// </summary>
        /// <param name="ast">The AST of the current document.</param>
        /// <param name="tokens">The tokens for the current document.</param>
        /// <returns>The helper object.</returns>
        private protected DocumentContextBuilder GetContextBuilder(Ast ast, Token[] tokens)
        {
            return new DocumentContextBuilder(ast, tokens);
        }

        /// <summary>
        /// Provides a more fluid way to build context for a refactor request.
        /// </summary>
        private protected class DocumentContextBuilder
        {
            private ThreadController _threadController;

            private IScriptExtent _selectionExtent;

            private IScriptPosition _cursorPosition;

            private CancellationToken? _cancellationToken;

            private PSCmdlet _cmdlet;

            internal DocumentContextBuilder(string scriptText)
            {
                Ast = Parser.ParseInput(scriptText, out Token[] tokens, out _);
                Tokens = tokens;
            }

            internal DocumentContextBuilder(Ast ast, Token[] tokens)
            {
                Ast = ast;
                Tokens = tokens;
            }

            internal Ast Ast { get; }

            internal Token[] Tokens { get; }

            internal IScriptExtent SelectionExtent
            {
                get
                {
                    return _selectionExtent ?? Empty.Extent.Untitled;
                }
            }

            internal IScriptPosition CursorPosition
            {
                get
                {
                    return _cursorPosition ?? Empty.Position.Untitled;
                }
            }

            internal CancellationToken CancellationToken
            {
                get
                {
                    return _cancellationToken ?? CancellationToken.None;
                }
            }

            /// <summary>
            /// Converts the builder into a usable <see cref="DocumentContextBase" /> object.
            /// </summary>
            /// <param name="builder">The builder to convert.</param>
            public static implicit operator DocumentContextBase(DocumentContextBuilder builder)
            {
                return new DocumentContext(
                    builder.Ast.FindRootAst(),
                    builder.Ast.FindAstAt(builder.CursorPosition),
                    new TokenCollection(builder.Tokens.AsMemory()).First
                        .FindNextOrSelf()
                        .ClosestTo(builder.CursorPosition)
                        .GetResult(),
                    builder.SelectionExtent,
                    builder._cmdlet,
                    builder._cancellationToken ?? CancellationToken.None,
                    builder._threadController);
            }

            /// <summary>
            /// Adds a <see cref="PSCmdlet" /> to the context.
            /// </summary>
            /// <param name="cmdlet">The <see cref="PSCmdlet" /> to add.</param>
            /// <returns>
            /// A reference to this instance after the operation has completed.
            /// </returns>
            public DocumentContextBuilder AddCmdlet(PSCmdlet cmdlet)
            {
                _cmdlet = cmdlet;
                return this;
            }

            /// <summary>
            /// Adds a <see cref="CancellationToken" /> to the context.
            /// </summary>
            /// <param name="cancellationToken">The token to add.</param>
            /// <returns>
            /// A reference to this instance after the operation has completed.
            /// </returns>
            public DocumentContextBuilder AddCancellationToken(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                return this;
            }

            /// <summary>
            /// Adds the current cursor position to the context.
            /// </summary>
            /// <param name="offset">The offset of the cursor location.</param>
            /// <returns>
            /// A reference to this instance after the operation has completed.
            /// </returns>
            public DocumentContextBuilder AddCursorPosition(int offset)
            {
                _cursorPosition = Ast.Extent.StartScriptPosition.CloneWithNewOffset(offset);
                return this;
            }

            /// <summary>
            /// Adds the current cursor position to the context.
            /// </summary>
            /// <param name="line">The line number of the cursor.</param>
            /// <param name="column">The column number of the cursor.</param>
            /// <returns>
            /// A reference to this instance after the operation has completed.
            /// </returns>
            public DocumentContextBuilder AddCursorPosition(int line, int column)
            {
                _cursorPosition = Ast.Extent.StartScriptPosition.CloneWithNewOffset(
                    PositionUtilities.GetOffsetFromPosition(Ast.Extent.Text, line, column));
                return this;
            }

            /// <summary>
            /// Adds the current cursor position to the context.
            /// </summary>
            /// <param name="position">
            /// The script position representing the location of the cursor.
            /// </param>
            /// <returns>
            /// A reference to this instance after the operation has completed.
            /// </returns>
            public DocumentContextBuilder AddCursorPosition(IScriptPosition position)
            {
                _cursorPosition = position;
                return this;
            }

            /// <summary>
            /// Adds the current range of selected text to the context.
            /// </summary>
            /// <param name="startOffset">The start of the selection.</param>
            /// <param name="endOffset">The end of the selection.</param>
            /// <returns>
            /// A reference to this instance after the operation has completed.
            /// </returns>
            public DocumentContextBuilder AddSelectionRange(int startOffset, int endOffset)
            {
                _selectionExtent = PositionUtilities.NewScriptExtent(
                    Ast.Extent,
                    startOffset,
                    endOffset);
                return this;
            }

            /// <summary>
            /// Adds the current range of selected text to the context.
            /// </summary>
            /// <param name="startLine">The starting line of the selection.</param>
            /// <param name="startColumn">The starting column of the selection.</param>
            /// <param name="endLine">The ending line of the selection.</param>
            /// <param name="endColumn">The ending column of the selection.</param>
            /// <returns>
            /// A reference to this instance after the operation has completed.
            /// </returns>
            public DocumentContextBuilder AddSelectionRange(int startLine, int startColumn, int endLine, int endColumn)
            {
                var lineMap = PositionUtilities.GetLineMap(Ast.Extent.Text);
                _selectionExtent = PositionUtilities.NewScriptExtent(
                    Ast.Extent,
                    PositionUtilities.GetOffsetFromPosition(
                        lineMap,
                        startLine,
                        startColumn),
                    PositionUtilities.GetOffsetFromPosition(
                        lineMap,
                        endLine,
                        endColumn));
                return this;
            }

            /// <summary>
            /// Adds the current range of selected text to the context.
            /// </summary>
            /// <param name="startPosition">
            /// The script position representing the start of the selection.
            /// </param>
            /// <param name="endPosition">
            /// The script position representing the end of the selection.
            /// </param>
            /// <returns>
            /// A reference to this instance after the operation has completed.
            /// </returns>
            public DocumentContextBuilder AddSelectionRange(IScriptPosition startPosition, IScriptPosition endPosition)
            {
                _selectionExtent = PositionUtilities.NewScriptExtent(
                    Ast.Extent,
                    startPosition.Offset,
                    endPosition.Offset);
                return this;
            }

            /// <summary>
            /// Adds the current range of selected text to the context.
            /// </summary>
            /// <param name="extent">
            /// The script extent representing the range of the selection.
            /// </param>
            /// <returns>
            /// A reference to this instance after the operation has completed.
            /// </returns>
            public DocumentContextBuilder AddSelectionRange(IScriptExtent extent)
            {
                _selectionExtent = extent;
                return this;
            }

            /// <summary>
            /// Adds the cronoller for the PowerShell pipeline thread.
            /// </summary>
            /// <param name="pipelineThread">
            /// The controller for the PowerShell pipeline thread.
            /// </param>
            /// <returns>
            /// A reference to this instance after the operation has completed.
            /// </returns>
            public DocumentContextBuilder AddThreadController(ThreadController pipelineThread)
            {
                _threadController = pipelineThread;
                return this;
            }
        }
    }
}
