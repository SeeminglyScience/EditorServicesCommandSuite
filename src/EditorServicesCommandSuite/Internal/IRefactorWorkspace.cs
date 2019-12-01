using System;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides information on the current workspace opened in the host editor.
    /// </summary>
    internal interface IRefactorWorkspace
    {
        /// <summary>
        /// Attempts to resolve a path relative to the root of the current workspace.
        /// </summary>
        /// <param name="path">The relative path to resolve.</param>
        /// <param name="doesExist">A value indicating whether the path exists.</param>
        /// <param name="resolvedPath">The full resolved path if successful.</param>
        /// <returns>
        /// A value indicating whether the path was able to be resolved.
        /// </returns>
        bool TryResolveRelativePath(
            string path,
            out bool doesExist,
            out string resolvedPath);

        /// <summary>
        /// Gets the path of the workspace opened in the host editor.
        /// </summary>
        /// <returns>The path.</returns>
        string GetWorkspacePath();

        /// <summary>
        /// Determines if the current workspace has a physical path associated with it.
        /// </summary>
        /// <returns>
        /// A value indicating whether the workspace is untitled.
        /// </returns>
        bool IsUntitledWorkspace();

        /// <summary>
        /// Attempts to get cached AST and token data for a script file from the editor host.
        /// </summary>
        /// <param name="path">
        /// The fully qualified or workspace relative path to the script file.
        /// </param>
        /// <param name="force">
        /// A value indicating whether an empty AST should be used as context
        /// if the file does not exist.
        /// </param>
        /// <param name="fileContext">
        /// The AST and token data for a script if found.
        /// </param>
        /// <returns>
        /// A value indicating whether AST and token data was successfully retrieved.
        /// </returns>
        bool TryGetFileContext(
            string path,
            bool force,
            out Tuple<ScriptBlockAst, Token[]> fileContext);

        /// <summary>
        /// Deletes the requested file.
        /// </summary>
        /// <param name="path">The file to be deleted.</param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous delete operation.
        /// </returns>
        Task DeleteFileAsync(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Moves the requested file.
        /// </summary>
        /// <param name="path">The file to moved.</param>
        /// <param name="destination">
        /// The location the file should be moved to. This should be the
        /// directory it will be moved to, not the full final path. The
        /// directory must exist.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous move operation.
        /// </returns>
        Task MoveFileAsync(string path, string destination, CancellationToken cancellationToken);

        /// <summary>
        /// Renames the request file.
        /// </summary>
        /// <param name="path">The file to be renamed.</param>
        /// <param name="newName">
        /// The new file name. This should be the file name only,
        /// not the full final path.
        /// </param>
        /// <param name="cancellationToken">
        /// The token to monitor for cancellation requests.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous rename operation.
        /// </returns>
        Task RenameFileAsync(string path, string newName, CancellationToken cancellationToken);
    }
}
