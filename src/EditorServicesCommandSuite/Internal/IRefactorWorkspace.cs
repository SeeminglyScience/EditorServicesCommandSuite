using System;
using System.Management.Automation.Language;

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
    }
}
