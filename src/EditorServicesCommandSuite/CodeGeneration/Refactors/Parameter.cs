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
            this._name = name;

            this._value = value;

            this._isMandatory = isMandatory;

            this._parameterType = parameterType.Name;

            char mandatoryMarkerOrSpace;

            if (this.IsMandatory)
            {
                mandatoryMarkerOrSpace = Symbols.Asterisk;
            }
            else
            {
                mandatoryMarkerOrSpace = Symbols.Space;
            }

            StringBuilder builder = new StringBuilder();
                builder
                    .Append(Symbols.Space, 2)
                    .Append(Symbols.NumberSign)
                    .Append(Symbols.Space)
                    .Append(mandatoryMarkerOrSpace)
                    .Append(Symbols.Space)
                    .Append(Symbols.SquareOpen)
                    .Append(this._parameterType)
                    .Append(Symbols.SquareClose);

            this._hint = builder.ToString();
        }

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public ParameterBindingResult Value
        {
            get
            {
                return _value;
            }
        }

        public bool IsMandatory
        {
            get
            {
                return _isMandatory;
            }
        }

        public string Hint
        {
            get
            {
                return _hint;
            }
        }

        private string _name;

        private ParameterBindingResult _value;

        private bool _isMandatory;

        private string _parameterType;

        private string _hint;
    }
}
