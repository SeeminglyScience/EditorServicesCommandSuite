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
        private readonly IPowerShellExecutor _executor;

        private readonly IRefactorUI _ui;

        private readonly IRefactorWorkspace _workspace;

        internal AddModuleQualificationRefactor(
            IPowerShellExecutor executor,
            IRefactorUI ui,
            IRefactorWorkspace workspace)
        {
            _executor = executor;
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
                await _ui.ShowErrorMessageAsync(
                    string.Format(
                        System.Globalization.CultureInfo.CurrentCulture,
                        AddModuleQualificationStrings.PSCmdletRequired,
                        nameof(AddModuleQualificationRefactor)),
                    waitForResponse: false);
                return Empty<DocumentEdit>.Array;
            }

            string commandName = ast.GetCommandName();
            if (string.IsNullOrWhiteSpace(commandName))
            {
                await ShowCommandNotFoundError(_ui);
                return Empty<DocumentEdit>.Array;
            }

            CommandInfo command = cmdlet.SessionState.InvokeCommand.GetCommand(
                commandName,
                CommandTypes.Function | CommandTypes.Cmdlet);

            request.CancellationToken.ThrowIfCancellationRequested();

            string moduleName;
            if (command != null)
            {
                if (!TryGetModuleNameFromCommand(command, out moduleName))
                {
                    return Empty<DocumentEdit>.Array;
                }

                return await GetEdits(ast, moduleName);
            }

            request.CancellationToken.ThrowIfCancellationRequested();
            ManifestInfo manifest;
            if (!ManifestInfo.TryGetWorkspaceManifest(_workspace, out manifest))
            {
                await ShowCommandNotFoundError(_ui);
                return Empty<DocumentEdit>.Array;
            }

            if (manifest.FunctionsToExport.Contains(commandName) ||
                manifest.CmdletsToExport.Contains(commandName) ||
                manifest.AliasesToExport.Contains(commandName))
            {
                return await GetEdits(ast, manifest.ModuleName);
            }

            request.CancellationToken.ThrowIfCancellationRequested();
            return Empty<DocumentEdit>.Array;
        }

        private static async Task ShowCommandNotFoundError(IRefactorUI ui)
        {
            await ui.ShowErrorMessageAsync(
                AddModuleQualificationStrings.CommandNameRequired,
                waitForResponse: false);
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
