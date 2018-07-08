using System.Management.Automation;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class CommandSplatRefactorSettings : RefactorConfiguration
    {
        [Parameter(Position = 0)]
        [DefaultFromSettingAttribute("CommandSplatRefactor.VariableName", Default = "$null")]
        [ValidateNotNullOrEmpty]
        public string VariableName { get; set; }

        [Parameter]
        [DefaultFromSettingAttribute("CommandSplatRefactor.NewLineAfterHashtable", Default = "$true")]
        public SwitchParameter NewLineAfterHashtable { get; set; }
    }
}
