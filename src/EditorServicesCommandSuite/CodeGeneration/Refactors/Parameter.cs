using System;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class Parameter
    {
        internal Parameter(
            string name,
            ParameterBindingResult value,
            bool isMandatory,
            Type parameterType)
        {
            char mandatoryMarkerOrSpace = isMandatory ? Symbols.Asterisk : Symbols.Space;
            var sb = new StringBuilder();

            sb
                .Append(Symbols.Space, 2)
                .Append(Symbols.NumberSign)
                .Append(Symbols.Space)
                .Append(mandatoryMarkerOrSpace)
                .Append(Symbols.Space)
                .Append(Symbols.SquareOpen)
                .Append(parameterType.Name)
                .Append(Symbols.SquareClose);

            Hint = sb.ToString();
            Name = name;
            Value = value;
            IsMandatory = isMandatory;
        }

        public string Name { get; }

        public ParameterBindingResult Value { get; }

        public bool IsMandatory { get; }

        public string Hint { get; }
    }
}
