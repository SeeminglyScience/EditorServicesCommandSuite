using System.Collections.ObjectModel;
using System.Management.Automation;

namespace EditorServicesCommandSuite.Internal
{
    public class WorkspaceContext : IRefactorWorkspace
    {
        protected readonly EngineIntrinsics Engine;

        protected internal WorkspaceContext()
        {
        }

        protected WorkspaceContext(EngineIntrinsics engine)
        {
            Engine = engine;
        }

        public virtual string GetWorkspacePath()
        {
            return string.Empty;
        }

        public virtual bool IsUntitledWorkspace()
        {
            return true;
        }

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
