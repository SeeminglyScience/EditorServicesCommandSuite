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
            splatWriter.Write(Symbols.Dollar);
            splatWriter.Write(variableName);
            splatWriter.Write(new[] { Symbols.Space, Symbols.Equal, Symbols.Space });
            splatWriter.OpenHashtable();

            var elementsWriter = new PowerShellScriptWriter(commandAst);
            elementsWriter.SetPosition(elementsExtent);
            elementsWriter.Write(Symbols.At);
            elementsWriter.Write(variableName);

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

            splatWriter.CloseHashtable();

            if (newLineAfterHashtable)
            {
                splatWriter.WriteLineNoIndent();
            }

            splatWriter.WriteLine();
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

            return await GetEdits(
                splatVariable,
                ast,
                config.NewLineAfterHashtable.IsPresent,
                UI);
        }

        private string GetSplatVariableName(CommandElementAst element)
        {
            var nameConstant = element as StringConstantExpressionAst;
            if (element == null)
            {
                return DefaultSplatVariable;
            }

            var variableName =
                nameConstant.Value
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
