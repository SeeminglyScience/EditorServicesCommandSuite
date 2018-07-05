using System;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class RefactorConfigurationAttribute : Attribute
    {
        internal RefactorConfigurationAttribute(Type configurationType)
        {
            ConfigurationType = configurationType;
        }

        internal Type ConfigurationType { get; }
    }
}
