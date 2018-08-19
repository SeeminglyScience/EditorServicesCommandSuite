using System.Management.Automation;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class CommandSplatRefactorSettings : RefactorConfiguration
    {
        [Parameter(Position = 0)]
        [DefaultFromSetting("CommandSplatRefactor.VariableName", Default = "$null")]
        [ValidateNotNullOrEmpty]
        public string VariableName { get; set; }

        [Parameter]
        [DefaultFromSetting("CommandSplatRefactor.NewLineAfterHashtable", Default = "$true")]
        public SwitchParameter NewLineAfterHashtable { get; set; }

        [Parameter]
        [DefaultFromSetting("CommandSplatRefactor.AllParameters", Default = "$false")]
        public SwitchParameter AllParameters { get; set; }

        [Parameter]
        [DefaultFromSetting("CommandSplatRefactor.MandatoryParameters", Default = "$false")]
        public SwitchParameter MandatoryParameters { get; set; }

        [Parameter]
        [DefaultFromSetting("CommandSplatRefactor.NoHints", Default = "$false")]
        public SwitchParameter NoHints { get; set; }
    }
}
