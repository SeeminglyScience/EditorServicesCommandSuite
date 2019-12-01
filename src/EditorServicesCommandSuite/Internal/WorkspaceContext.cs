using System;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides the default <see cref="IRefactorWorkspace" /> implementation.
    /// </summary>
    internal class WorkspaceContext : IRefactorWorkspace
    {
        private const string FileSystemProviderQualifier = "Microsoft.PowerShell.Core\\FileSystem::";

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
        /// Gets the PowerShell engine associated with the session.
        /// </summary>
        protected EngineIntrinsics Engine { get; }

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
        /// <param name="doesExist">A value indicating whether the path exists.</param>
        /// <param name="resolvedPath">The full resolved path if successful.</param>
        /// <returns>
        /// A value indicating whether the path was able to be resolved.
        /// </returns>
        public virtual bool TryResolveRelativePath(
            string path,
            out bool doesExist,
            out string resolvedPath)
        {
            if (IsUntitledWorkspace() ||
                Engine == null ||
                string.IsNullOrWhiteSpace(path))
            {
                resolvedPath = string.Empty;
                doesExist = false;
                return false;
            }

            string workspacePath = null;
            try
            {
                workspacePath = GetWorkspacePath();
            }
#pragma warning disable RCS1075
            catch (Exception)
#pragma warning restore RCS1075
            {
                // Treat any exception as an untitled workspace.
            }

            if (string.IsNullOrEmpty(workspacePath))
            {
                resolvedPath = string.Empty;
                doesExist = false;
                return false;
            }

            string unresolvedPath = Engine.SessionState.Path.Combine(workspacePath, path);
            resolvedPath = Engine.SessionState.Path.GetUnresolvedProviderPathFromPSPath(
                unresolvedPath,
                out _,
                out _);

            doesExist = Engine.SessionState.InvokeProvider.Item.Exists(unresolvedPath);
            return true;
        }

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
        public virtual bool TryGetFileContext(
            string path,
            bool force,
            out Tuple<ScriptBlockAst, Token[]> fileContext)
        {
            if (IsUntitledWorkspace())
            {
                fileContext = null;
                return false;
            }

            path = GetUnresolvedProviderPath(path, out bool doesExist);
            if (path == null)
            {
                fileContext = null;
                return false;
            }

            if (!doesExist)
            {
                if (force)
                {
                    fileContext = Tuple.Create(
                        (ScriptBlockAst)Empty.ScriptAst.Get(path),
                        Array.Empty<Token>());
                    return true;
                }

                fileContext = null;
                return false;
            }

            fileContext = GetFileContext(path);
            return fileContext != null;
        }

        public virtual Task DeleteFileAsync(string path, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public virtual Task MoveFileAsync(string path, string destination, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public virtual Task RenameFileAsync(string path, string newName, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Inheriting classes can override this method to customize how AST and token
        /// data is retrieved. If the editor host already has this information, this
        /// method should be overriden to pull from the host's internal cache.
        /// </summary>
        /// <param name="path">Rooted path to the script file requested.</param>
        /// <returns>
        /// AST and token data for the script file or <see langword="null" /> if invalid.
        /// </returns>
        protected virtual Tuple<ScriptBlockAst, Token[]> GetFileContext(string path)
        {
            return Tuple.Create(
                Parser.ParseFile(path, out Token[] tokens, out _),
                tokens);
        }

        private string GetUnresolvedProviderPath(string path, out bool doesExist)
        {
            PathIntrinsics pathIntrinsics = Engine.SessionState.Path;
            if (pathIntrinsics.IsPSAbsolute(path, out _))
            {
                string providerQualifiedPath = pathIntrinsics.IsProviderQualified(path)
                    ? path
                    : string.Concat(FileSystemProviderQualifier, path);

                path = pathIntrinsics.GetUnresolvedProviderPathFromPSPath(
                    providerQualifiedPath,
                    out ProviderInfo provider,
                    out _);

                // A provider qualified path was specified for a provider other than the file system.
                if (provider.ImplementingType != typeof(Microsoft.PowerShell.Commands.FileSystemProvider))
                {
                    doesExist = false;
                    return null;
                }

                doesExist = Engine.InvokeProvider.Item.Exists(
                    providerQualifiedPath,
                    force: true,
                    literalPath: true);

                return path;
            }

            if (TryResolveRelativePath(path, out doesExist, out path))
            {
                return path;
            }

            doesExist = false;
            return null;
        }
    }
}
