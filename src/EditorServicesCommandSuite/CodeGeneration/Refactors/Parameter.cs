using System;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class Parameter
    {
        internal Parameter(
            string name,
            ParameterBindingResult value)
        {
            Name = name;
            Value = value;
        }

        internal Parameter(ParameterMetadata metadata, ParameterSetMetadata setMetadata)
        {
            Name = metadata.Name;
            IsMandatory = setMetadata.IsMandatory;
            Type = metadata.ParameterType;
        }

        public string Name { get; }

        public ParameterBindingResult Value { get; }

        public bool IsMandatory { get; }

        public Type Type { get; }
    }
}
