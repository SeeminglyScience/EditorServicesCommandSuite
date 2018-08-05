using System;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration
{
    internal static class Write
    {
        internal static void AsExpressionValue(PowerShellScriptWriter writer, ParameterBindingResult element)
        {
            if (element == null)
            {
                return;
            }

            if (element.ConstantValue is bool boolean)
            {
                writer.Write(Symbols.Dollar + boolean.ToString().ToLower());
                return;
            }

            if (element.Value is StringConstantExpressionAst ||
                element.Value is ExpandableStringExpressionAst)
            {
                dynamic stringExpression = element.Value;
                if (stringExpression.StringConstantType != StringConstantType.BareWord)
                {
                    writer.Write(element.Value.Extent.Text);
                    return;
                }

                if (element.Value is StringConstantExpressionAst constant)
                {
                    writer.WriteStringExpression(
                        StringConstantType.SingleQuoted,
                        constant.Value);
                    return;
                }

                writer.WriteStringExpression(
                    StringConstantType.DoubleQuoted,
                    element.Value.Extent.Text);
                return;
            }

            writer.WriteEachWithSeparator(
                element.Value.Extent.Text.Split(
                    new[] { writer.NewLine },
                    StringSplitOptions.None),
                line => writer.Write(line),
                () => writer.WriteLine());
        }
    }
}
