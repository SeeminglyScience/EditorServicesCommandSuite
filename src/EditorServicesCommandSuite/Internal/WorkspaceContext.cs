using System.Collections.ObjectModel;
using System.Management.Automation;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides the default <see cref="IRefactorWorkspace" /> implementation.
    /// </summary>
    public class WorkspaceContext : IRefactorWorkspace
    {
        /// <summary>
        /// The PowerShell engine associated with the session.
        /// </summary>
        protected readonly EngineIntrinsics Engine;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceContext" /> class.
        /// </summary>
        protected internal WorkspaceContext()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkspaceContext" /> class.
        /// </summary>
        /// <param name="engine">The PowerShell engine for the session.</param>
        protected WorkspaceContext(EngineIntrinsics engine)
        {
            Engine = engine;
        }

        /// <summary>
        /// Gets the path of the workspace opened in the host editor.
        /// </summary>
        /// <returns>
        /// The path of the workspace if the implementation overrides the method, otherwise an empty string.
        /// </returns>
        public virtual string GetWorkspacePath()
        {
            return string.Empty;
        }

        /// <summary>
        /// Determines if the current workspace has a physical path associated with it.
        /// </summary>
        /// <returns>
        /// A value indicating whether the workspace is untitled if the implement overrides
        /// the method, otherwise it always returns false.
        /// </returns>
        public virtual bool IsUntitledWorkspace()
        {
            return true;
        }

        /// <summary>
        /// Attempts to resolve a path relative to the root of the current workspace.
        /// </summary>
        /// <param name="path">The relative path to resolve.</param>
        /// <param name="resolvedPath">The full resolved path if successful.</param>
        /// <returns>
        /// A value indicating whether the path was able to be resolved.
        /// </returns>
        public virtual bool TryResolveRelativePath(string path, out string resolvedPath)
        {
            if (IsUntitledWorkspace() ||
                Engine == null ||
                string.IsNullOrWhiteSpace(path))
            {
                resolvedPath = string.Empty;
                return false;
            }

            string workspacePath = null;
            try
            {
                workspacePath = GetWorkspacePath();
            }
            catch (System.Exception)
            {
            }

            if (string.IsNullOrEmpty(workspacePath))
            {
                resolvedPath = string.Empty;
                return false;
            }

            Collection<string> resolvedPaths;
            ProviderInfo provider;
            try
            {
                resolvedPaths = Engine.SessionState.Path.GetResolvedProviderPathFromPSPath(
                    Engine.SessionState.Path.Combine(
                        workspacePath,
                        path),
                    out provider);
            }
            catch (ItemNotFoundException)
            {
                resolvedPath = null;
                return false;
            }

            resolvedPath = resolvedPaths[0];
            return true;
        }
    }
}
