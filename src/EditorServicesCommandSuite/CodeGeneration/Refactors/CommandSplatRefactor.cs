using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsData.ConvertTo, "SplatExpression")]
    [RefactorConfiguration(typeof(CommandSplatRefactorSettings))]
    internal class CommandSplatRefactor : AstRefactorProvider<CommandAst>
    {
        private const string DefaultSplatVariable = "splat";

        private const string SplatVariableSuffix = "Splat";

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
            bool newLineAfterHashtable,
            bool allParameters,
            bool mandatoryParameters,
            bool noHints,
            EngineIntrinsics executionContext,
            IRefactorUI ui = null)
        {
            var parentStatement = commandAst.FindParent<StatementAst>();
            var commandName = commandAst.GetCommandName();
            var elements = commandAst.CommandElements.Skip(1);
            var elementsExtent = elements.JoinExtents();
            var boundParameters = StaticParameterBinder.BindCommand(commandAst, resolve: true);

            if (boundParameters.BindingExceptions.TryGetValue(commandName, out StaticBindingError globalError) &&
                globalError.BindingException.ErrorId.Equals("AmbiguousParameterSet", StringComparison.Ordinal))
            {
                if (ui != null)
                {
                    await ui.ShowErrorMessageAsync(globalError.BindingException.Message);
                    return Enumerable.Empty<DocumentEdit>();
                }

                throw new PSInvalidOperationException(globalError.BindingException.Message, globalError.BindingException);
            }

            if (boundParameters.BoundParameters.Count == 0 &&
                !(allParameters || mandatoryParameters))
            {
                return Enumerable.Empty<DocumentEdit>();
            }

            var commandInfo = executionContext
                    .InvokeCommand
                    .GetCommand(commandName, CommandTypes.All);

            var parameterSetName = ResolveParameterSet(boundParameters, commandInfo);

            var parameterInfo = commandInfo.ParameterSets
                .FirstOrDefault(set => set.Name.Equals(parameterSetName, StringComparison.Ordinal))
                ?.Parameters;

            List<Parameter> parameterList = new List<Parameter>();

            foreach (var param in parameterInfo)
            {
                var shouldAdd = false;
                ParameterBindingResult boundParameterValue = null;

                if (allParameters)
                {
                    shouldAdd = true;
                }

                if (mandatoryParameters && param.IsMandatory)
                {
                    shouldAdd = true;
                }

                if (Cmdlet.CommonParameters.Contains(param.Name) || Cmdlet.OptionalCommonParameters.Contains(param.Name))
                {
                    shouldAdd = false;
                }

                if (boundParameters.BoundParameters.ContainsKey(param.Name))
                {
                    boundParameters.BoundParameters.TryGetValue(
                        param.Name,
                        out boundParameterValue);
                }

                if (boundParameterValue != null || shouldAdd)
                {
                    parameterList.Add(
                        new Parameter(
                            param.Name,
                            boundParameterValue,
                            param.IsMandatory,
                            param.ParameterType));
                }
            }

            var splatWriter = new PowerShellScriptWriter(commandAst);
            var elementsWriter = new PowerShellScriptWriter(commandAst);

            splatWriter.SetPosition(parentStatement);
            splatWriter.WriteAssignment(
                () => splatWriter.WriteVariable(variableName),
                () => splatWriter.OpenHashtable());

            if (elementsExtent is EmptyExtent)
            {
                elementsWriter.SetPosition(parentStatement, atEnd: true);
                elementsWriter.Write(Symbols.Space);
            }
            else
            {
                elementsWriter.SetPosition(elementsExtent);
            }

            elementsWriter.WriteVariable(variableName, isSplat: true);

            var first = true;
            foreach (var param in parameterList)
            {
                if (param.Name.All(c => char.IsDigit(c)))
                {
                    elementsWriter.Write(
                        Symbols.Space
                        + param.Value.Value.Extent.Text);
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
                    param.Name,
                    () => splatWriter.WriteAsExpressionValue(param, noHints));
            }

            foreach (var bindingException in boundParameters.BindingExceptions)
            {
                elementsWriter.Write(Symbols.Space);
                elementsWriter.Write(bindingException.Value.CommandElement.Extent.Text);

                // The ui?.ShowWarningMessageAsync() pattern does not work during testing. Await does not seem to like null.
                if (ui != null)
                {
                    await ui.ShowWarningMessageAsync(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            CommandSplatStrings.CouldNotResolvePositionalArgument,
                            bindingException.Value.CommandElement.Extent.Text),
                        waitForResponse: false);
                }
            }

            splatWriter.CloseHashtable();

            if (newLineAfterHashtable)
            {
                splatWriter.WriteLine();
            }

            splatWriter.WriteLine();
            splatWriter.WriteIndentIfPending();
            splatWriter.CreateDocumentEdits();
            elementsWriter.CreateDocumentEdits(elementsExtent.Text.Length);
            return splatWriter.Edits.Concat(elementsWriter.Edits);
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
            var executionContext = CommandSuite.Instance.ExecutionContext;

            return await GetEdits(
                splatVariable,
                ast,
                config.NewLineAfterHashtable.IsPresent,
                config.AllParameters.IsPresent,
                config.MandatoryParameters.IsPresent,
                config.NoHints.IsPresent,
                executionContext,
                UI);
        }

        private static string ResolveParameterSet(
            StaticBindingResult paramBinder,
            CommandInfo commandInfo)
        {
            if (commandInfo.ParameterSets.Count == 1)
            {
                return commandInfo.ParameterSets[0].Name;
            }

            foreach (CommandParameterSetInfo parameterSet in commandInfo.ParameterSets)
            {
                var currentSetParameterNames = new HashSet<string>(parameterSet.Parameters.Select(p => p.Name));
                var isMatch = true;
                foreach (string parameterName in paramBinder.BoundParameters.Keys)
                {
                    if (!currentSetParameterNames.Contains(parameterName))
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
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
    }
}
