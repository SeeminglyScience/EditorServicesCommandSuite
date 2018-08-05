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
        /// <summary>
        /// The automatic name of the parameter set when no other sets are defined: "__AllParameterSets".
        /// Also the set in which mutual parameters fall when more than one set is defined.
        /// </summary>
        private const string AutomaticSingleParameterSetName = "__AllParameterSets";
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
            /*
            High level design
                1. Get parameters from Ast, resolve (positional, partial) parameters with StaticParameterBinder.
                2. Resolve parameterset, [to be able to determine if a param is mandatory]
                3. Gather relevant parameter data: name, value, isMandatory, type
                    3a. (optional) add other parameters from CommandInfo + parameter set name. name, value, isMandatory, type
                    3b. (optional) filter out non-mandatory params
                4. Get longest parametername [to be able to format all '=' signs]
                5. Sort // do we want a certain order?
                6. Write parameters, values (if present), and (optional) type hint
                    //  --> Parameter class moet iets van een ToWriteAssignment ofzo krijgen zie class Write, en class PowerShellScriptWriter.
                    // naast 'internal void WriteHashtableEntry(string key, Action valueWriter)' moet er ook een komen met uitgelijnde '='.
                7. Display parameter binding errors

            */

            // TODO: implement showHints switchparameter, with a logical default setting.
            bool showHints = true;

            // 1. Get parameters from Ast, resolve (positional, partial) parameters with StaticParameterBinder.
            var parentStatement = commandAst.FindParent<StatementAst>();
            var elements = commandAst.CommandElements.Skip(1);

            var elementsExtent = elements.JoinExtents();
            var boundParameters = StaticParameterBinder.BindCommand(commandAst, true);
            if (
                boundParameters.BindingExceptions.Count() > 0 &&
                boundParameters.BindingExceptions
                    .ToArray()
                    .First()
                    .Value
                    .BindingException
                    .ErrorId == "AmbiguousParameterSet")
            {
                throw boundParameters.BindingExceptions
                    .ToArray()
                    .First()
                    .Value
                    .BindingException;
            }

            if (
                !boundParameters.BoundParameters.Any() &&
                !(allParameters || mandatoryParameters))
            {
                return Enumerable.Empty<DocumentEdit>();
            }

            // 2. Resolve parameterset, [to be able to determine if a param is mandatory]
            var cmdName = commandAst.CommandElements[0].Extent.Text;

            var cmdInfo =
                CommandSuite
                    .Instance
                    .ExecutionContext
                    .InvokeCommand
                    .GetCommand(cmdName, CommandTypes.All);

            var parameterSetName = ResolveParameterSet(boundParameters, cmdInfo);

            // 3. Gather relevant parameter data: name, value, isMandatory, type
            var parameterInfo = cmdInfo
                .ParameterSets
                .Where(p => parameterSetName == p.Name)
                .SelectMany(p => p.Parameters);

            List<Parameter> pList = new List<Parameter>();

            foreach (var param in parameterInfo)
            {
                // 3a. (optional) add other parameters from CommandInfo + parameter set name. name, value, isMandatory, type
                // 3b. (optional) filter out non-mandatory params
                var add = false;
                ParameterBindingResult boundParameterValue = null;

                if (allParameters)
                {
                    add = true;
                }

                if (mandatoryParameters && param.IsMandatory)
                {
                    add = true;
                }

                // Omit common parameters and optional common parameters
                if (Cmdlet.CommonParameters.Contains(param.Name) || Cmdlet.OptionalCommonParameters.Contains(param.Name))
                {
                    add = false;
                }

                if (boundParameters.BoundParameters.ContainsKey(param.Name))
                {
                    boundParameterValue =
                        boundParameters
                            .BoundParameters
                            .Where(p => param.Name == p.Key)
                            .FirstOrDefault()
                            .Value;
                }

                // Always add parameter if it was bound.
                if (boundParameterValue != null || add)
                {
                    pList.Add(
                        new Parameter(
                            param.Name,
                            boundParameterValue,
                            param.IsMandatory,
                            param.ParameterType));
                }
            }

            // 4. Get longest parametername [to be able to format all '=' signs]
            var equalSignAligner =
                pList
                    .Select(p => p.Name.Length)
                    .Max();

            // 5. Sort
            IEnumerable<Parameter> sorted;
            /*
            Do we want a certain order?
            No additional sorting will have params appear in the order provided by CommandInfo. The previous implementation had all bound
            parameters first, in order as typed, and, if (allParameters || matchedParameters), the rest of them in order of appearance in CommandInfo.
            These are thought experiments on what kind of sorting could be usefull, the 'sorted' variable is dereferenced and never used.
            */

            // Bound parameters first, rest in order of appearance
            sorted =
                from element in pList
                orderby element.Value != null
                select element;

            // Alphabetical:
            sorted =
                from element in pList
                orderby element.Name
                select element;

            // Alphabetical, with Mandatory parameters first:
            sorted =
                from element in pList
                orderby element.IsMandatory, element.Name
                select element;

            sorted = null;

            // 6. Write parameters, values (if present), and (optional) type hint
            var splatWriter = new PowerShellScriptWriter(commandAst);
            var elementsWriter = new PowerShellScriptWriter(commandAst);

            splatWriter.SetPosition(parentStatement);
            splatWriter.WriteAssignment(
                () => splatWriter.WriteVariable(variableName),
                () => splatWriter.OpenHashtable());

            if (elementsExtent is EmptyExtent)
            {
                elementsWriter.SetPosition(parentStatement, true);
                elementsWriter.Write(Symbols.Space);
            }
            else
            {
                elementsWriter.SetPosition(elementsExtent);
            }

            elementsWriter.WriteVariable(variableName, isSplat: true);

            var first = true;
            foreach (var param in pList)
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
                    () => Write.AsExpressionValue(splatWriter, param.Value),
                    equalSignAligner);

                if (showHints)
                {
                    splatWriter.Write(param.Hint);
                }
            }

            // 7. Display parameter binding errors
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

        internal static string ResolveParameterSet(
            StaticBindingResult paramBinder,
            CommandInfo commandInfo)
        {
            IEnumerable<string> matchedParameterSet = new List<string>()
            {
                AutomaticSingleParameterSetName,
            };

            if (commandInfo.ParameterSets.Count > 1)
            {
                // Identify parameters that are specific to (a) certain parameterset(s)
                IEnumerable<ParameterMetadata> specificParams =
                    commandInfo
                        .Parameters
                        .Values
                        .Where(p => !p.ParameterSets.ContainsKey(AutomaticSingleParameterSetName));

                // Try and match against one single parameterset
                // This wil return null if certain parameters are in more than one parameterset, or if none of the specificParams where bound.
                matchedParameterSet =
                    specificParams
                        .Where(p => paramBinder.BoundParameters.ContainsKey(p.Name) && p.ParameterSets.Count == 1)
                        .Select(p => p.ParameterSets.Keys.ToArray().First());

                // If matching a single parameterset failed, return only default parameterset
                if (matchedParameterSet.Count() == 0)
                {
                    matchedParameterSet = commandInfo.ParameterSets.Where(p => p.IsDefault).Select(n => n.Name);
                }
            }

            return matchedParameterSet.FirstOrDefault();
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
