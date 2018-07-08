namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides information on the current workspace opened in the host editor.
    /// </summary>
    public interface IRefactorWorkspace
    {
        /// <summary>
        /// Attempts to resolve a path relative to the root of the current workspace.
        /// </summary>
        /// <param name="path">The relative path to resolve.</param>
        /// <param name="resolvedPath">The full resolved path if successful.</param>
        /// <returns>
        /// A value indicating whether the path was able to be resolved.
        /// </returns>
        bool TryResolveRelativePath(string path, out string resolvedPath);

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
    }
}
