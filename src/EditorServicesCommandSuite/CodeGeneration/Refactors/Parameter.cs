using System;
using System.Management.Automation.Language;

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
            Name = name;
            Value = value;
            IsMandatory = isMandatory;
            Type = parameterType;
        }

        public string Name { get; }

        public ParameterBindingResult Value { get; }

        public bool IsMandatory { get; }

        public Type Type { get; }
    }
}
