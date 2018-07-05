using System.Management.Automation;

namespace EditorServicesCommandSuite.Reflection
{
    internal class ParameterDescription
    {
        internal ParameterDescription(string name, PSTypeName parameterType)
        {
            Name = name;
            ParameterType = parameterType;
        }

        public string Name { get; }

        public PSTypeName ParameterType { get; }
    }
}
