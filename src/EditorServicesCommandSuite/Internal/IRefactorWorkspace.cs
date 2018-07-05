namespace EditorServicesCommandSuite.Internal
{
    public interface IRefactorWorkspace
    {
        bool TryResolveRelativePath(string path, out string resolvedPath);

        string GetWorkspacePath();

        bool IsUntitledWorkspace();
    }
}
