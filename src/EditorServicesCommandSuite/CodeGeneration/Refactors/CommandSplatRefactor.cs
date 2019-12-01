using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsData.ConvertTo, "SplatExpression")]
    [RefactorConfiguration(typeof(CommandSplatRefactorSettings))]
    internal class CommandSplatRefactor : RefactorProvider
    {
        private const string DefaultSplatVariable = "splat";

        private const string SplatVariableSuffix = "Splat";

        private const string AmbiguousParameterSet = "AmbiguousParameterSet";

        private static readonly HashSet<string> s_allCommonParameters =
            new HashSet<string>(
                Cmdlet.CommonParameters.Concat(Cmdlet.OptionalCommonParameters),
                StringComparer.OrdinalIgnoreCase);

        internal CommandSplatRefactor(IRefactorUI ui)
        {
            UI = ui;
        }

        public override string Name { get; } = CommandSplatStrings.ProviderDisplayName;

        public override string Description { get; } = CommandSplatStrings.ProviderDisplayDescription;

        public override ImmutableArray<CodeAction> SupportedActions { get; } = ImmutableArray.Create(
            CodeAction.Inactive(CodeActionIds.CommandSplat, "Use splat expression"),
            CodeAction.Inactive(CodeActionIds.CommandSplatAllParameters, "Use splat expression - add all parameters"),
            CodeAction.Inactive(CodeActionIds.CommandSplatMandatoryParameters, "Use splat expression - add mandatory parameters"));

        internal IRefactorUI UI { get; }

        private CodeAction DefaultSplatCodeAction => SupportedActions[0];

        private CodeAction SplatAllParametersCodeAction => SupportedActions[1];

        private CodeAction SplatMandatoryParametersCodeAction => SupportedActions[2];

        public override async Task ComputeCodeActions(DocumentContextBase context)
        {
            if (!context.Ast.TryFindParent(maxDepth: 3, out CommandAst command))
            {
                return;
            }

            bool hasSplattableArgs = false;
            foreach (CommandElementAst commandElement in command.CommandElements.Skip(1))
            {
                if (commandElement is VariableExpressionAst variable && variable.Splatted)
                {
                    continue;
                }

                hasSplattableArgs = true;
            }

            if (hasSplattableArgs)
            {
                await context.RegisterCodeActionAsync(
                    CreateCodeAction(command, AdditionalParameterTypes.None))
                    .ConfigureAwait(false);
            }

            await context.RegisterCodeActionAsync(
                CreateCodeAction(command, AdditionalParameterTypes.Mandatory))
                .ConfigureAwait(false);

            await context.RegisterCodeActionAsync(
                CreateCodeAction(command, AdditionalParameterTypes.All))
                .ConfigureAwait(false);
        }

        internal static async Task SplatCommandAsync(
            DocumentContextBase context,
            CommandAst command,
            AdditionalParameterTypes parameterTypes,
            IRefactorUI ui)
        {
            StaticBindingResult bindingResult = await context.PipelineThread.InvokeAsync(
                () => StaticParameterBinder.BindCommand(command, resolve: true))
                .ConfigureAwait(false);

            var config = context.GetConfiguration<CommandSplatRefactorSettings>();
            string splatVariable = string.IsNullOrWhiteSpace(config.VariableName)
                ? GetSplatVariableName(command.CommandElements[0])
                : config.VariableName;

            IEnumerable<DocumentEdit> edits = await GetEdits(
                new CommandSplatArguments(
                    context.PipelineThread,
                    ui,
                    bindingResult,
                    command,
                    splatVariable,
                    parameterTypes,
                    config.ExcludeHints,
                    config.NewLineAfterHashtable,
                    context.CancellationToken))
                    .ConfigureAwait(false);

            await context.RegisterWorkspaceChangeAsync(
                WorkspaceChange.EditDocument(
                    context.Document,
                    edits))
                .ConfigureAwait(false);
        }

        private static async Task<(List<Parameter>, List<StaticBindingError>)> ProcessBindingResult(
            CommandSplatArguments args,
            string commandName)
        {
            List<Parameter> parameterList = new List<Parameter>();
            foreach (KeyValuePair<string, ParameterBindingResult> parameter in args.BindingResult.BoundParameters)
            {
                parameterList.Add(new Parameter(parameter.Key, parameter.Value));
            }

            if (args.IncludedTypes != AdditionalParameterTypes.None)
            {
                await AddAdditionalParameters(
                    args,
                    commandName,
                    parameterList)
                    .ConfigureAwait(false);
            }

            List<StaticBindingError> unresolvedPositionalArgs = new List<StaticBindingError>();
            StaticBindingResult unresolvedBinding = null;
            foreach (var bindingException in args.BindingResult.BindingExceptions)
            {
                bool isPositional = bindingException.Value.BindingException.ErrorId.Equals(
                    "PositionalParameterNotFound",
                    StringComparison.Ordinal);

                if (isPositional)
                {
                    unresolvedPositionalArgs.Add(bindingException.Value);
                    continue;
                }

                if (unresolvedBinding == null)
                {
                    unresolvedBinding = StaticParameterBinder.BindCommand(args.Command, resolve: false);
                }

                bool isBound = unresolvedBinding.BoundParameters.TryGetValue(
                    bindingException.Key,
                    out ParameterBindingResult unresolvedResult);

                if (isBound)
                {
                    parameterList.Add(
                        new Parameter(
                            bindingException.Key,
                            unresolvedResult));
                }
            }

            return (parameterList, unresolvedPositionalArgs);
        }

        private static async Task<IEnumerable<DocumentEdit>> GetEdits(CommandSplatArguments args)
        {
            PipelineAst parentStatement = args.Command.FindParent<PipelineAst>();
            string commandName = args.Command.GetCommandName();
            IEnumerable<CommandElementAst> elements = args.Command.CommandElements.Skip(1);
            IScriptExtent elementsExtent = elements.JoinExtents();

            List<Parameter> parameterList;
            List<StaticBindingError> unresolvedPositionalArgs;
            (parameterList, unresolvedPositionalArgs) =
                await ProcessBindingResult(args, commandName).ConfigureAwait(false);

            if (parameterList.Count == 0)
            {
                return Enumerable.Empty<DocumentEdit>();
            }

            var splatWriter = new PowerShellScriptWriter(args.Command);
            var elementsWriter = new PowerShellScriptWriter(args.Command);

            splatWriter.SetPosition(parentStatement);
            splatWriter.WriteAssignment(
                () => splatWriter.WriteVariable(args.VariableName),
                () => splatWriter.OpenHashtable());

            if (elementsExtent is Empty.Extent)
            {
                elementsWriter.SetPosition(parentStatement, atEnd: true);
                elementsWriter.Write(Symbols.Space);
            }
            else
            {
                elementsWriter.SetPosition(elementsExtent);
            }

            elementsWriter.WriteVariable(args.VariableName, isSplat: true);

            var first = true;
            foreach (Parameter parameter in parameterList)
            {
                if (parameter.Name.All(c => char.IsDigit(c)))
                {
                    elementsWriter.Write(Symbols.Space);
                    elementsWriter.Write(parameter.Value.Value.Extent.Text);
                    continue;
                }

                if (first)
                {
                    first = false;
                }
                else
                {
                    splatWriter.WriteLine();
                }

                splatWriter.WriteHashtableEntry(
                    parameter.Name,
                    () => splatWriter.WriteAsExpressionValue(parameter, args.ExcludeHints));
            }

            foreach (StaticBindingError positionalParameter in unresolvedPositionalArgs)
            {
                elementsWriter.Write(Symbols.Space);
                elementsWriter.Write(positionalParameter.CommandElement.Extent.Text);

                await args.UI.ShowWarningMessageAsync(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        CommandSplatStrings.CouldNotResolvePositionalArgument,
                        positionalParameter.CommandElement.Extent.Text))
                    .ConfigureAwait(false);
            }

            splatWriter.CloseHashtable();

            if (args.NewLineAfterHashtable)
            {
                splatWriter.WriteLine();
            }

            splatWriter.WriteLine();
            splatWriter.WriteIndentIfPending();
            splatWriter.CreateDocumentEdits();
            elementsWriter.CreateDocumentEdits(elementsExtent.Text.Length);
            return splatWriter.Edits.Concat(elementsWriter.Edits);
        }

        private static async Task AddAdditionalParameters(
            CommandSplatArguments args,
            string commandName,
            List<Parameter> parameterList)
        {
            bool wasAmbiguousSet =
                args.BindingResult.BindingExceptions.TryGetValue(commandName, out StaticBindingError globalError)
                && globalError.BindingException.ErrorId.Equals(AmbiguousParameterSet, StringComparison.Ordinal);

            if (wasAmbiguousSet)
            {
                await args.UI.ShowErrorMessageOrThrowAsync(
                    Error.InvalidOperation,
                    globalError.BindingException.Message)
                    .ConfigureAwait(false);
            }

            // Need to also get parameter from the main thread as the getter will marshal the call
            // back via the PSEventManager.
            CommandInfo commandInfo;
            Dictionary<string, ParameterMetadata> parameters;
            ReadOnlyCollection<CommandParameterSetInfo> parameterSets;

            (commandInfo, parameters, parameterSets) =
                await args.PipelineThread.InvokeAsync(
                    (EngineIntrinsics engine) =>
                    {
                        CommandInfo cmd = engine.InvokeCommand.GetCommand(commandName, CommandTypes.All);
                        return (cmd, cmd.Parameters, cmd.ParameterSets);
                    }).ConfigureAwait(false);

            if (commandInfo == null)
            {
                await args.UI.ShowErrorMessageOrThrowAsync(
                    Error.CommandNotFound,
                    commandName)
                    .ConfigureAwait(false);
            }

            string parameterSetName = ResolveParameterSet(args.BindingResult, commandInfo, parameterSets);
            foreach (ParameterMetadata parameter in parameters.Values)
            {
                args.CancellationToken.ThrowIfCancellationRequested();
                if (s_allCommonParameters.Contains(parameter.Name))
                {
                    continue;
                }

                if (args.BindingResult.BoundParameters.ContainsKey(parameter.Name))
                {
                    continue;
                }

                if (!(parameter.ParameterSets.TryGetValue(parameterSetName, out ParameterSetMetadata setMetadata) ||
                    parameter.ParameterSets.TryGetValue(ParameterAttribute.AllParameterSets, out setMetadata)))
                {
                    continue;
                }

                if (args.IncludedTypes != AdditionalParameterTypes.All && !setMetadata.IsMandatory)
                {
                    continue;
                }

                parameterList.Add(new Parameter(parameter, setMetadata));
            }
        }

        private static string ResolveParameterSet(
            StaticBindingResult bindingResult,
            CommandInfo commandInfo,
            ReadOnlyCollection<CommandParameterSetInfo> parameterSets)
        {
            if (parameterSets.Count == 1)
            {
                return parameterSets[0].Name;
            }

            var parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var candidates = new List<string>();
            foreach (CommandParameterSetInfo parameterSet in parameterSets)
            {
                parameterNames.Clear();
                foreach (CommandParameterInfo parameter in parameterSet.Parameters)
                {
                    parameterNames.Add(parameter.Name);
                }

                if (parameterNames.IsSupersetOf(bindingResult.BoundParameters.Keys))
                {
                    candidates.Add(parameterSet.Name);
                }
            }

            if (candidates.Count == 0)
            {
                return ParameterAttribute.AllParameterSets;
            }

            if (TryGetDefaultParameterSet(commandInfo, out string defaultParameterSetName) &&
                candidates.Contains(defaultParameterSetName))
            {
                return defaultParameterSetName;
            }

            return candidates[0];
        }

        private static bool TryGetDefaultParameterSet(CommandInfo commandInfo, out string defaultParameterSetName)
        {
            if (commandInfo is CmdletInfo cmdlet && !string.IsNullOrEmpty(cmdlet.DefaultParameterSet))
            {
                defaultParameterSetName = cmdlet.DefaultParameterSet;
                return true;
            }

            if (commandInfo is FunctionInfo function && !string.IsNullOrEmpty(function.DefaultParameterSet))
            {
                defaultParameterSetName = function.DefaultParameterSet;
                return true;
            }

            defaultParameterSetName = null;
            return false;
        }

        private static string GetSplatVariableName(CommandElementAst element)
        {
            var nameConstant = element as StringConstantExpressionAst;
            if (element == null)
            {
                return DefaultSplatVariable;
            }

            string constantValue = nameConstant.Value;
            if (constantValue.Contains(Symbols.Backslash) || constantValue.Contains(Symbols.ForwardSlash))
            {
                // Command appears to be module qualified, so try to determine the actual command name.
                constantValue = System.Text.RegularExpressions.Regex
                    .Split(constantValue, @"\\|/")
                    .Last();
            }

            var variableName =
                constantValue
                    .Replace(Symbols.Dash.ToString(), string.Empty)
                    + SplatVariableSuffix;

            if (!char.IsUpper(variableName[0]))
            {
                return variableName;
            }

            return
                char.ToLower(variableName[0])
                + variableName.Substring(1);
        }

        private CodeAction CreateCodeAction(
            CommandAst command,
            AdditionalParameterTypes parameterTypes)
        {
            CodeAction action = parameterTypes switch
            {
                AdditionalParameterTypes.All => SplatAllParametersCodeAction,
                AdditionalParameterTypes.Mandatory => SplatMandatoryParametersCodeAction,
                AdditionalParameterTypes.None => DefaultSplatCodeAction,
                _ => throw new PSArgumentOutOfRangeException(nameof(parameterTypes)),
            };

            return action.With(
                factory: SplatCommandAsync,
                state: (command, parameterTypes, UI));
        }

        private struct CommandSplatArguments
        {
            internal readonly ThreadController PipelineThread;

            internal readonly IRefactorUI UI;

            internal readonly CancellationToken CancellationToken;

            internal readonly StaticBindingResult BindingResult;

            internal readonly CommandAst Command;

            internal readonly string VariableName;

            internal readonly AdditionalParameterTypes IncludedTypes;

            internal readonly bool ExcludeHints;

            internal readonly bool NewLineAfterHashtable;

            internal CommandSplatArguments(
                ThreadController pipelineThread,
                IRefactorUI ui,
                StaticBindingResult bindingResult,
                CommandAst commandAst,
                string variableName,
                AdditionalParameterTypes includedParameterTypes,
                bool excludeHints,
                bool newLineAfterHashtable,
                CancellationToken cancellationToken)
            {
                PipelineThread = pipelineThread;
                UI = ui;
                BindingResult = bindingResult;
                Command = commandAst;
                VariableName = variableName;
                IncludedTypes = includedParameterTypes;
                ExcludeHints = excludeHints;
                NewLineAfterHashtable = newLineAfterHashtable;
                CancellationToken = cancellationToken;
            }
        }
    }
}
