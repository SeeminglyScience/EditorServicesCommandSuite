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
            IRefactorUI ui = null)
        {
            var parentStatement = commandAst.FindParent<StatementAst>();
            var elements = commandAst.CommandElements.Skip(1);

            var elementsExtent = elements.JoinExtents();
            var boundParameters = StaticParameterBinder.BindCommand(commandAst, true);
            if (!boundParameters.BoundParameters.Any())
            {
                return Enumerable.Empty<DocumentEdit>();
            }

            var splatWriter = new PowerShellScriptWriter(commandAst);
            splatWriter.SetPosition(parentStatement);
            splatWriter.WriteAssignment(
                () => splatWriter.WriteVariable(variableName),
                () => splatWriter.OpenHashtable());

            var elementsWriter = new PowerShellScriptWriter(commandAst);
            elementsWriter.SetPosition(elementsExtent);
            elementsWriter.WriteVariable(variableName, isSplat: true);

            var first = true;
            foreach (var param in boundParameters.BoundParameters)
            {
                if (param.Key.All(c => char.IsDigit(c)))
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
                    param.Key,
                    () => Write.AsExpressionValue(splatWriter, param.Value));
            }

            foreach (var bindingException in boundParameters.BindingExceptions)
            {
                elementsWriter.Write(Symbols.Space);
                elementsWriter.Write(bindingException.Value.CommandElement.Extent.Text);

                await ui?.ShowWarningMessageAsync(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        CommandSplatStrings.CouldNotResolvePositionalArgument,
                        bindingException.Value.CommandElement.Extent.Text),
                    waitForResponse: false);
            }

            if (allParameters || mandatoryParameters)
            {
                IEnumerable<CommandParameterInfo> parameterList;
                var cmdName = commandAst.CommandElements[0].Extent.Text;
                var cmdInfo =
                    CommandSuite
                        .Instance
                        .ExecutionContext
                        .InvokeCommand
                        .GetCommand(cmdName, CommandTypes.All);

                if (cmdInfo.ParameterSets.Count == 1)
                {
                    parameterList =
                        cmdInfo
                            .ParameterSets
                            .SelectMany(p => p.Parameters);
                }
                else {
                    parameterList =
                        GetParametersInMatchedParameterSet(boundParameters, cmdInfo);
                }

                if (mandatoryParameters)
                {
                    parameterList = parameterList.Where(p => p.IsMandatory);
                }

                // omit common parameters and optional common parameters
                parameterList =
                    parameterList
                        .Where(p => !Cmdlet.CommonParameters.Contains(p.Name) && !Cmdlet.OptionalCommonParameters.Contains(p.Name));

                // omit parameters that were already bound.
                parameterList = parameterList.Where(p => !boundParameters.BoundParameters.Keys.Contains(p.Name));

                foreach (string param in parameterList.Select(p => p.Name))
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        splatWriter.WriteLine();
                    }

                    splatWriter.Write(param);
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

        private static IEnumerable<CommandParameterInfo> GetParametersInMatchedParameterSet(
            StaticBindingResult paramBinder,
            CommandInfo cmdInfo)
        {
            List<CommandParameterInfo> result = new List<CommandParameterInfo>();

            // parameters that are specific to (a) certain parameterset(s)
            IEnumerable<ParameterMetadata> specificParams =
                cmdInfo
                    .Parameters
                    .Values
                    .Where(p => !(p.ParameterSets.ContainsKey("__AllParameterSets")));

            // try and match against one single parameterset (this wil return null if certain parameters are in more than one parameterset)
            IEnumerable<string> matchedParameterSet =
                specificParams
                    .Where(p => paramBinder.BoundParameters.ContainsKey(p.Name) && p.ParameterSets.Count == 1)
                    .Select(p => p.ParameterSets.Keys.ToArray().First());

            // if matching a single parameterset failed, return all possible parametersets.
            if (matchedParameterSet == null)
            {
                matchedParameterSet = cmdInfo.ParameterSets.Where(p => p.IsDefault).Select(n => n.Name);
            }
            else if (matchedParameterSet.Count() > 1)
            {
                // TODO: Possible invalid parameter combination. This may be worth a PowerShell console warning. But is this ever hit?
            }

            // return parameters from matched parameterset(s)
            return cmdInfo
                        .ParameterSets
                        .Where(p => matchedParameterSet.Contains(p.Name))   // parameters from __AllParameterSets are implicitly included here.
                        .SelectMany(p => p.Parameters);
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
                config.NewLineAfterHashtable.IsPresent,
                config.AllParameters.IsPresent,
                config.MandatoryParameters.IsPresent,
                UI);
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
