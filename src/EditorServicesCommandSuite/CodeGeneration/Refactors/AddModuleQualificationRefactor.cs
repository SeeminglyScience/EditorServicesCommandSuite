using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsCommon.Add, "ModuleQualification")]
    internal class AddModuleQualificationRefactor : RefactorProvider
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

        public override string Name => AddModuleQualificationStrings.ProviderDisplayName;

        public override string Description => AddModuleQualificationStrings.ProviderDisplayDescription;

        public override ImmutableArray<CodeAction> SupportedActions { get; } = ImmutableArray.Create(
            CodeAction.Inactive(
                CodeActionIds.AddModuleQualification,
                AddModuleQualificationStrings.ProviderDisplayDescription));

        public override async Task ComputeCodeActions(DocumentContextBase context)
        {
            if (!context.Ast.TryFindParent(maxDepth: 3, out CommandAst command))
            {
                return;
            }

            string name = command.GetCommandName();
            if (string.IsNullOrEmpty(name) || name.Contains(Symbols.Backslash))
            {
                return;
            }

            await context.RegisterCodeActionAsync(
                SupportedActions[0].With(AddQualificationAsync, (command, name)))
                .ConfigureAwait(false);
        }

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

        internal static WorkspaceChange GetWorkspaceChange(
            string document,
            CommandAst ast,
            string moduleName)
        {
            var writer = new PowerShellScriptWriter(ast);
            writer.SetPosition(ast);
            writer.Write(moduleName);
            writer.Write(Symbols.Backslash);
            writer.CreateDocumentEdits(overwriteCount: 0);
            return WorkspaceChange.EditDocument(document, writer.Edits);
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

        private async Task AddQualificationAsync(
            DocumentContextBase context,
            CommandAst commandAst,
            string commandName)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (!context.TryGetCmdlet(out PSCmdlet cmdlet))
            {
                await _ui.ShowErrorMessageOrThrowAsync(Error.CmdletRequired)
                    .ConfigureAwait(false);
                return;
            }

            CommandInfo command = await context.PipelineThread.InvokeAsync(
                () => cmdlet.SessionState.InvokeCommand.GetCommand(
                    commandName,
                    CommandTypes.Function | CommandTypes.Cmdlet),
                context.CancellationToken)
                .ConfigureAwait(false);

            if (command != null)
            {
                if (!TryGetModuleNameFromCommand(command, out string moduleName))
                {
                    return;
                }

                await context.RegisterWorkspaceChangeAsync(
                    GetWorkspaceChange(context.Document, commandAst, moduleName))
                    .ConfigureAwait(false);
                return;
            }

            context.CancellationToken.ThrowIfCancellationRequested();
            if (!ManifestInfo.TryGetWorkspaceManifest(_workspace, out ManifestInfo manifest))
            {
                await _ui.ShowErrorMessageOrThrowAsync(Error.CommandNotFound).ConfigureAwait(false);
                return;
            }

            if (manifest.FunctionsToExport.Contains(commandName) ||
                manifest.CmdletsToExport.Contains(commandName) ||
                manifest.AliasesToExport.Contains(commandName))
            {
                await context.RegisterWorkspaceChangeAsync(
                    GetWorkspaceChange(context.Document, commandAst, manifest.ModuleName))
                    .ConfigureAwait(false);
            }
        }
    }
}
