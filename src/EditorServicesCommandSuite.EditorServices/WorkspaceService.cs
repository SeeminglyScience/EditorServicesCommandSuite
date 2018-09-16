using System;
using System.Management.Automation;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class WorkspaceService : WorkspaceContext
    {
        private readonly Workspace _workspace;

        internal WorkspaceService(EngineIntrinsics engine, Workspace workspace)
            : base(engine)
        {
            _workspace = workspace;
        }

        public override string GetWorkspacePath() => _workspace.WorkspacePath;

        public override bool IsUntitledWorkspace()
        {
            return string.IsNullOrEmpty(_workspace.WorkspacePath);
        }

        protected override Tuple<ScriptBlockAst, Token[]> GetFileContext(string path)
        {
            ScriptFile scriptFile = _workspace.GetFile(path);
            return Tuple.Create(scriptFile.ScriptAst, scriptFile.ScriptTokens);
        }
    }
}
