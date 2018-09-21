using System;
using System.Collections.Generic;
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
    internal class CommandSplatRefactor : AstRefactorProvider<CommandAst>
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

        internal IRefactorUI UI { get; }

        internal static async Task<IEnumerable<DocumentEdit>> GetEdits(
            string variableName,
            CommandAst commandAst,
            AdditionalParameterTypes includedParameterTypes,
            bool newLineAfterHashtable,
            bool excludeHints,
            ThreadController pipelineThread,
            CancellationToken cancellationToken,
            IRefactorUI ui = null)
        {
            StaticBindingResult bindingResult = await pipelineThread.InvokeAsync(
                () => StaticParameterBinder.BindCommand(commandAst, resolve: true));

            return await GetEdits(
                new CommandSplatArguments(
                    pipelineThread,
                    ui,
                    bindingResult,
                    commandAst,
                    variableName,
                    includedParameterTypes,
                    excludeHints,
                    newLineAfterHashtable,
                    cancellationToken));
        }

        internal override bool CanRefactorTarget(DocumentContextBase request, CommandAst ast)
        {
            return
                ast.CommandElements.Count > 1 &&
                !ast.CommandElements.Any(
                    element =>
                        element is VariableExpressionAst variable
                        && variable.Splatted);
        }

        internal override async Task<IEnumerable<DocumentEdit>> RequestEdits(DocumentContextBase request, CommandAst ast)
        {
            var config = request.GetConfiguration<CommandSplatRefactorSettings>();
            var splatVariable = string.IsNullOrWhiteSpace(config.VariableName)
                ? GetSplatVariableName(ast.CommandElements.First())
                : config.VariableName;

            return await GetEdits(
                splatVariable,
                ast,
                config.AdditionalParameters,
                config.NewLineAfterHashtable,
                config.ExcludeHints,
                request.PipelineThread,
                request.CancellationToken,
                UI);
        }

        private static async Task<IEnumerable<DocumentEdit>> GetEdits(CommandSplatArguments args)
        {
            StatementAst parentStatement = args.Command.FindParent<StatementAst>();
            string commandName = args.Command.GetCommandName();
            IEnumerable<CommandElementAst> elements = args.Command.CommandElements.Skip(1);
            IScriptExtent elementsExtent = elements.JoinExtents();

            if (args.BindingResult.BoundParameters.Count == 0 &&
                !(args.IncludedTypes == AdditionalParameterTypes.Mandatory ||
                args.IncludedTypes == AdditionalParameterTypes.All))
            {
                return Enumerable.Empty<DocumentEdit>();
            }

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
                    parameterList);
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

            foreach (var bindingException in args.BindingResult.BindingExceptions)
            {
                elementsWriter.Write(Symbols.Space);
                elementsWriter.Write(bindingException.Value.CommandElement.Extent.Text);

                await args.UI.ShowWarningMessageAsync(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        CommandSplatStrings.CouldNotResolvePositionalArgument,
                        bindingException.Value.CommandElement.Extent.Text));
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
            if (args.BindingResult.BindingExceptions.TryGetValue(commandName, out StaticBindingError globalError) &&
                globalError.BindingException.ErrorId.Equals(AmbiguousParameterSet, StringComparison.Ordinal))
            {
                await args.UI.ShowErrorMessageOrThrowAsync(
                    Error.InvalidOperation,
                    globalError.BindingException.Message);
            }

            CommandInfo commandInfo =
                await args.PipelineThread.InvokeAsync(
                    (EngineIntrinsics engine) => engine
                        .InvokeCommand
                        .GetCommand(commandName, CommandTypes.All));

            if (commandInfo == null)
            {
                await args.UI.ShowErrorMessageOrThrowAsync(
                    Error.CommandNotFound,
                    commandName);
            }

            string parameterSetName = ResolveParameterSet(args.BindingResult, commandInfo);
            foreach (ParameterMetadata parameter in commandInfo.Parameters.Values)
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

                ParameterSetMetadata setMetadata;
                if (!(parameter.ParameterSets.TryGetValue(parameterSetName, out setMetadata) ||
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
            CommandInfo commandInfo)
        {
            if (commandInfo.ParameterSets.Count == 1)
            {
                return commandInfo.ParameterSets[0].Name;
            }

            var parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CommandParameterSetInfo parameterSet in commandInfo.ParameterSets)
            {
                parameterNames.Clear();
                foreach (CommandParameterInfo parameter in parameterSet.Parameters)
                {
                    parameterNames.Add(parameter.Name);
                }

                if (parameterNames.IsSupersetOf(bindingResult.BoundParameters.Keys))
                {
                    return parameterSet.Name;
                }
            }

            return ParameterAttribute.AllParameterSets;
        }

        private string GetSplatVariableName(CommandElementAst element)
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
