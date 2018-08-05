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

        [Parameter]
        [DefaultFromSettingAttribute("CommandSplatRefactor.AllParameters", Default = "$false")]
        public SwitchParameter AllParameters { get; set; }

        [Parameter]
        [DefaultFromSettingAttribute("CommandSplatRefactor.MandatoryParameters", Default = "$false")]
        public SwitchParameter MandatoryParameters { get; set; }

        [Parameter]
        [DefaultFromSettingAttribute("CommandSplatRefactor.NoHints", Default = "$false")]
        public SwitchParameter NoHints { get; set; }
    }
}
