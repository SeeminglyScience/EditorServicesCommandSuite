using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.Internal
{
    public abstract class DocumentContextProvider
    {
        protected internal abstract string Workspace { get; }

        protected internal abstract Task<DocumentContextBase> GetDocumentContextAsync();

        protected internal abstract Task<DocumentContextBase> GetDocumentContextAsync(PSCmdlet cmdlet);

        protected internal abstract Task<DocumentContextBase> GetDocumentContextAsync(CancellationToken cancellationToken);

        protected internal abstract Task<DocumentContextBase> GetDocumentContextAsync(PSCmdlet cmdlet, CancellationToken cancellationToken);

        protected DocumentContextBuilder GetContextBuilder(string scriptText)
        {
            return new DocumentContextBuilder(scriptText);
        }

        protected DocumentContextBuilder GetContextBuilder(Ast ast, Token[] tokens)
        {
            return new DocumentContextBuilder(ast, tokens);
        }

        protected class DocumentContextBuilder
        {
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
                    if (_selectionExtent == null)
                    {
                        return PositionUtilities.EmptyExtent;
                    }

                    return _selectionExtent;
                }
            }

            internal IScriptPosition CursorPosition
            {
                get
                {
                    if (_cursorPosition == null)
                    {
                        return PositionUtilities.EmptyPosition;
                    }

                    return _cursorPosition;
                }
            }

            internal CancellationToken CancellationToken
            {
                get
                {
                    if (!_cancellationToken.HasValue)
                    {
                        return CancellationToken.None;
                    }

                    return _cancellationToken.Value;
                }
            }

            public static implicit operator DocumentContextBase(DocumentContextBuilder builder)
            {
                return new DocumentContext(
                    builder.Ast.FindRootAst(),
                    builder.Ast.FindAstAt(builder.CursorPosition),
                    new LinkedList<Token>(builder.Tokens).First.At(builder.CursorPosition),
                    builder.SelectionExtent,
                    builder._cmdlet,
                    builder._cancellationToken ?? CancellationToken.None);
            }

            public DocumentContextBuilder AddCmdlet(PSCmdlet cmdlet)
            {
                _cmdlet = cmdlet;
                return this;
            }

            public DocumentContextBuilder AddCancellationToken(CancellationToken cancellationToken)
            {
                _cancellationToken = cancellationToken;
                return this;
            }

            public DocumentContextBuilder AddCursorPosition(int offset)
            {
                _cursorPosition = Ast.Extent.StartScriptPosition.CloneWithNewOffset(offset);
                return this;
            }

            public DocumentContextBuilder AddCursorPosition(int line, int column)
            {
                _cursorPosition = Ast.Extent.StartScriptPosition.CloneWithNewOffset(
                    PositionUtilities.GetOffsetFromPosition(Ast.Extent.Text, line, column));
                return this;
            }

            public DocumentContextBuilder AddCursorPosition(IScriptPosition position)
            {
                _cursorPosition = position;
                return this;
            }

            public DocumentContextBuilder AddSelectionRange(int startOffset, int endOffset)
            {
                _selectionExtent = PositionUtilities.NewScriptExtent(
                    Ast.Extent,
                    startOffset,
                    endOffset);
                return this;
            }

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

            public DocumentContextBuilder AddSelectionRange(IScriptPosition startPosition, IScriptPosition endPosition)
            {
                _selectionExtent = PositionUtilities.NewScriptExtent(
                    Ast.Extent,
                    startPosition.Offset,
                    endPosition.Offset);
                return this;
            }

            public DocumentContextBuilder AddSelectionRange(IScriptExtent extent)
            {
                _selectionExtent = extent;
                return this;
            }
        }
    }
}
