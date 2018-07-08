using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides the ability to alter the user's current position within a document.
    /// </summary>
    public abstract class NavigationService : IRefactorNavigation
    {
        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="line">The new line number.</param>
        /// <param name="column">The new column number.</param>
        public virtual void SetCursorPosition(int line, int column)
        {
            SetCursorPosition(line, column, CancellationToken.None);
        }

        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="line">The new line number.</param>
        /// <param name="column">The new column number.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        public abstract void SetCursorPosition(int line, int column, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="position">
        /// The script position representing the new cursor location.
        /// </param>
        public virtual void SetCursorPosition(IScriptPosition position)
        {
            SetCursorPosition(position.LineNumber, position.ColumnNumber, CancellationToken.None);
        }

        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="position">
        /// The script position representing the new cursor location.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        public virtual void SetCursorPosition(IScriptPosition position, CancellationToken cancellationToken)
        {
            SetCursorPosition(position.LineNumber, position.ColumnNumber, cancellationToken);
        }

        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="line">The new line number.</param>
        /// <param name="column">The new column number.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        public virtual Task SetCursorPositionAsync(int line, int column)
        {
            return SetCursorPositionAsync(line, column, CancellationToken.None);
        }

        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="line">The new line number.</param>
        /// <param name="column">The new column number.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        public abstract Task SetCursorPositionAsync(int line, int column, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="position">
        /// The script position representing the new cursor location.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        public virtual Task SetCursorPositionAsync(IScriptPosition position)
        {
            return SetCursorPositionAsync(position.LineNumber, position.ColumnNumber, CancellationToken.None);
        }

        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="position">
        /// The script position representing the new cursor location.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        public virtual Task SetCursorPositionAsync(IScriptPosition position, CancellationToken cancellationToken)
        {
            return SetCursorPositionAsync(
                position.LineNumber,
                position.ColumnNumber,
                cancellationToken);
        }

        /// <summary>
        /// Sets the selection range.
        /// </summary>
        /// <param name="startLine">
        /// The line number of the selection start.
        /// </param>
        /// <param name="startColumn">
        /// The column number of the selection start.
        /// </param>
        /// <param name="endLine">
        /// The line number of the selection end.
        /// </param>
        /// <param name="endColumn">
        /// The column number of the selection end.
        /// </param>
        public virtual void SetSelection(int startLine, int startColumn, int endLine, int endColumn)
        {
            SetSelection(
                startLine,
                startColumn,
                endLine,
                endColumn,
                CancellationToken.None);
        }

        /// <summary>
        /// Sets the selection range.
        /// </summary>
        /// <param name="startLine">
        /// The line number of the selection start.
        /// </param>
        /// <param name="startColumn">
        /// The column number of the selection start.
        /// </param>
        /// <param name="endLine">
        /// The line number of the selection end.
        /// </param>
        /// <param name="endColumn">
        /// The column number of the selection end.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        public abstract void SetSelection(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the selection range.
        /// </summary>
        /// <param name="extent">
        /// The script extent representing the entire range of the new selection.
        /// </param>
        public virtual void SetSelection(IScriptExtent extent)
        {
            SetSelection(
                extent.StartLineNumber,
                extent.StartColumnNumber,
                extent.EndLineNumber,
                extent.EndColumnNumber,
                CancellationToken.None);
        }

        /// <summary>
        /// Sets the selection range.
        /// </summary>
        /// <param name="extent">
        /// The script extent representing the entire range of the new selection.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        public virtual void SetSelection(IScriptExtent extent, CancellationToken cancellationToken)
        {
            SetSelection(
                extent.StartLineNumber,
                extent.StartColumnNumber,
                extent.EndLineNumber,
                extent.EndColumnNumber,
                cancellationToken);
        }

        /// <summary>
        /// Sets the selection range.
        /// </summary>
        /// <param name="startLine">
        /// The line number of the selection start.
        /// </param>
        /// <param name="startColumn">
        /// The column number of the selection start.
        /// </param>
        /// <param name="endLine">
        /// The line number of the selection end.
        /// </param>
        /// <param name="endColumn">
        /// The column number of the selection end.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        public virtual Task SetSelectionAsync(int startLine, int startColumn, int endLine, int endColumn)
        {
            return SetSelectionAsync(
                startLine,
                startColumn,
                endLine,
                endColumn,
                CancellationToken.None);
        }

        /// <summary>
        /// Sets the selection range.
        /// </summary>
        /// <param name="startLine">
        /// The line number of the selection start.
        /// </param>
        /// <param name="startColumn">
        /// The column number of the selection start.
        /// </param>
        /// <param name="endLine">
        /// The line number of the selection end.
        /// </param>
        /// <param name="endColumn">
        /// The column number of the selection end.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        public abstract Task SetSelectionAsync(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the selection range.
        /// </summary>
        /// <param name="extent">
        /// The script extent representing the entire range of the new selection.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        public virtual Task SetSelectionAsync(IScriptExtent extent)
        {
            return SetSelectionAsync(
                extent.StartLineNumber,
                extent.StartColumnNumber,
                extent.EndLineNumber,
                extent.EndColumnNumber,
                CancellationToken.None);
        }

        /// <summary>
        /// Sets the selection range.
        /// </summary>
        /// <param name="extent">
        /// The script extent representing the entire range of the new selection.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
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
