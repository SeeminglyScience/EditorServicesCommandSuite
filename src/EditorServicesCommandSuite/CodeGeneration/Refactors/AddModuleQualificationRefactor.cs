using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsCommon.Add, "ModuleQualification")]
    internal class AddModuleQualificationRefactor : AstRefactorProvider<CommandAst>
    {
        private readonly IRefactorUI _ui;

        private readonly IRefactorWorkspace _workspace;

        internal AddModuleQualificationRefactor(
            IRefactorUI ui,
            IRefactorWorkspace workspace)
        {
            _ui = ui;
            _workspace = workspace;
        }

        public override string Name { get; } = AddModuleQualificationStrings.ProviderDisplayName;

        public override string Description { get; } = AddModuleQualificationStrings.ProviderDisplayDescription;

        internal static Task<IEnumerable<DocumentEdit>> GetEdits(
            CommandAst ast,
            string moduleName)
        {
            var writer = new PowerShellScriptWriter(ast);
            writer.SetPosition(ast);
            writer.Write(moduleName);
            writer.Write(Symbols.Backslash);
            writer.CreateDocumentEdits(overwriteCount: 0);
            return Task.FromResult(writer.Edits);
        }

        internal override bool CanRefactorTarget(DocumentContextBase request, CommandAst ast)
        {
            bool alreadyQualifiedOrNoName = ast.GetCommandName()?.Contains(Symbols.Backslash) ?? true;
            return !alreadyQualifiedOrNoName;
        }

        internal override async Task<IEnumerable<DocumentEdit>> RequestEdits(
            DocumentContextBase request,
            CommandAst ast)
        {
            request.CancellationToken.ThrowIfCancellationRequested();
            PSCmdlet cmdlet = null;
            if (!request.TryGetCmdlet(out cmdlet))
            {
                await _ui.ShowErrorMessageOrThrowAsync(
                    Error.CmdletRequired,
                    nameof(AddModuleQualificationRefactor));

                return Array.Empty<DocumentEdit>();
            }

            string commandName = ast.GetCommandName();
            if (string.IsNullOrWhiteSpace(commandName))
            {
                await _ui.ShowErrorMessageOrThrowAsync(Error.CommandNotFound);
                return Array.Empty<DocumentEdit>();
            }

            CommandInfo command = await request.PipelineThread.InvokeAsync(
                () => cmdlet.SessionState.InvokeCommand.GetCommand(
                    commandName,
                    CommandTypes.Function | CommandTypes.Cmdlet),
                request.CancellationToken);

            string moduleName;
            if (command != null)
            {
                if (!TryGetModuleNameFromCommand(command, out moduleName))
                {
                    return Array.Empty<DocumentEdit>();
                }

                return await GetEdits(ast, moduleName);
            }

            request.CancellationToken.ThrowIfCancellationRequested();
            ManifestInfo manifest;
            if (!ManifestInfo.TryGetWorkspaceManifest(_workspace, out manifest))
            {
                await _ui.ShowErrorMessageOrThrowAsync(Error.CommandNotFound);
                return Array.Empty<DocumentEdit>();
            }

            if (manifest.FunctionsToExport.Contains(commandName) ||
                manifest.CmdletsToExport.Contains(commandName) ||
                manifest.AliasesToExport.Contains(commandName))
            {
                return await GetEdits(ast, manifest.ModuleName);
            }

            request.CancellationToken.ThrowIfCancellationRequested();
            return Array.Empty<DocumentEdit>();
        }

        private static bool TryGetModuleNameFromCommand(CommandInfo command, out string moduleName)
        {
            if (command == null)
            {
                moduleName = null;
                return false;
            }

            if (!string.IsNullOrWhiteSpace(command.ModuleName))
            {
                moduleName = command.ModuleName;
                return true;
            }

            if (command.Module == null)
            {
                moduleName = null;
                return false;
            }

            moduleName = command.Module.Name;
            return !string.IsNullOrWhiteSpace(moduleName);
        }
    }
}
