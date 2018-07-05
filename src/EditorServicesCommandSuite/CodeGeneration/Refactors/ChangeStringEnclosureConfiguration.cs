using System.Management.Automation;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class ChangeStringEnclosureConfiguration : RefactorConfiguration
    {
        [Parameter]
        public StringEnclosureType Type { get; set; }
    }
}
