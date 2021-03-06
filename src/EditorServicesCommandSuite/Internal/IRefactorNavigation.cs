using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides the ability to alter the user's current position within a document.
    /// </summary>
    internal interface IRefactorNavigation
    {
        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="line">The new line number.</param>
        /// <param name="column">The new column number.</param>
        void SetCursorPosition(int line, int column);

        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="line">The new line number.</param>
        /// <param name="column">The new column number.</param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        void SetCursorPosition(int line, int column, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="position">
        /// The script position representing the new cursor location.
        /// </param>
        void SetCursorPosition(IScriptPosition position);

        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="position">
        /// The script position representing the new cursor location.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        void SetCursorPosition(IScriptPosition position, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="line">The new line number.</param>
        /// <param name="column">The new column number.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        Task SetCursorPositionAsync(int line, int column);

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
        Task SetCursorPositionAsync(int line, int column, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the position of the cursor.
        /// </summary>
        /// <param name="position">
        /// The script position representing the new cursor location.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        Task SetCursorPositionAsync(IScriptPosition position);

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
        Task SetCursorPositionAsync(IScriptPosition position, CancellationToken cancellationToken);

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
        void SetSelection(int startLine, int startColumn, int endLine, int endColumn);

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
        void SetSelection(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the selection range.
        /// </summary>
        /// <param name="extent">
        /// The script extent representing the entire range of the new selection.
        /// </param>
        void SetSelection(IScriptExtent extent);

        /// <summary>
        /// Sets the selection range.
        /// </summary>
        /// <param name="extent">
        /// The script extent representing the entire range of the new selection.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token that will be checked prior to completing the returned task.
        /// </param>
        void SetSelection(IScriptExtent extent, CancellationToken cancellationToken);

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
        Task SetSelectionAsync(int startLine, int startColumn, int endLine, int endColumn);

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
        Task SetSelectionAsync(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken);

        /// <summary>
        /// Sets the selection range.
        /// </summary>
        /// <param name="extent">
        /// The script extent representing the entire range of the new selection.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        Task SetSelectionAsync(IScriptExtent extent);

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
        Task SetSelectionAsync(IScriptExtent extent, CancellationToken cancellationToken);
    }
}
