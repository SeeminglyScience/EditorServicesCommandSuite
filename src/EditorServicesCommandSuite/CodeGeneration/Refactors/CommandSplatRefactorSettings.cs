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
        [DefaultFromSetting("CommandSplatRefactor.AdditionalParameters", Default = "\"None\"")]
        public AdditionalParameterTypes AdditionalParameters { get; set; }

        [Parameter]
        [DefaultFromSetting("CommandSplatRefactor.ExcludeHints", Default = "$false")]
        public SwitchParameter ExcludeHints { get; set; }
    }
}
