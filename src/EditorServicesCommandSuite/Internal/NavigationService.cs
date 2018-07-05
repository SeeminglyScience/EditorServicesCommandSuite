using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    public abstract class NavigationService : IRefactorNavigation
    {
        public virtual void SetCursorPosition(int line, int column)
        {
            SetCursorPosition(line, column, CancellationToken.None);
        }

        public abstract void SetCursorPosition(int line, int column, CancellationToken cancellationToken);

        public virtual void SetCursorPosition(IScriptPosition position)
        {
            SetCursorPosition(position.LineNumber, position.ColumnNumber, CancellationToken.None);
        }

        public virtual void SetCursorPosition(IScriptPosition position, CancellationToken cancellationToken)
        {
            SetCursorPosition(position.LineNumber, position.ColumnNumber, cancellationToken);
        }

        public virtual Task SetCursorPositionAsync(int line, int column)
        {
            return SetCursorPositionAsync(line, column, CancellationToken.None);
        }

        public abstract Task SetCursorPositionAsync(int line, int column, CancellationToken cancellationToken);

        public virtual Task SetCursorPositionAsync(IScriptPosition position)
        {
            return SetCursorPositionAsync(position.LineNumber, position.ColumnNumber, CancellationToken.None);
        }

        public virtual Task SetCursorPositionAsync(IScriptPosition position, CancellationToken cancellationToken)
        {
            return SetCursorPositionAsync(
                position.LineNumber,
                position.ColumnNumber,
                cancellationToken);
        }

        public virtual void SetSelection(int startLine, int startColumn, int endLine, int endColumn)
        {
            SetSelection(
                startLine,
                startColumn,
                endLine,
                endColumn,
                CancellationToken.None);
        }

        public abstract void SetSelection(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken);

        public virtual void SetSelection(IScriptExtent extent)
        {
            SetSelection(
                extent.StartLineNumber,
                extent.StartColumnNumber,
                extent.EndLineNumber,
                extent.EndColumnNumber,
                CancellationToken.None);
        }

        public virtual void SetSelection(IScriptExtent extent, CancellationToken cancellationToken)
        {
            SetSelection(
                extent.StartLineNumber,
                extent.StartColumnNumber,
                extent.EndLineNumber,
                extent.EndColumnNumber,
                cancellationToken);
        }

        public virtual Task SetSelectionAsync(int startLine, int startColumn, int endLine, int endColumn)
        {
            return SetSelectionAsync(
                startLine,
                startColumn,
                endLine,
                endColumn,
                CancellationToken.None);
        }

        public abstract Task SetSelectionAsync(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken);

        public virtual Task SetSelectionAsync(IScriptExtent extent)
        {
            return SetSelectionAsync(
                extent.StartLineNumber,
                extent.StartColumnNumber,
                extent.EndLineNumber,
                extent.EndColumnNumber,
                CancellationToken.None);
        }

        public virtual Task SetSelectionAsync(IScriptExtent extent, CancellationToken cancellationToken)
        {
            return SetSelectionAsync(
                extent.StartLineNumber,
                extent.StartColumnNumber,
                extent.EndLineNumber,
                extent.EndColumnNumber,
                cancellationToken);
        }
    }
}
