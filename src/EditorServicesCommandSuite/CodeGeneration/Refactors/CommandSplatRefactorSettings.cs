using System.Management.Automation;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class CommandSplatRefactorSettings : RefactorConfiguration
    {
        [Parameter(Position = 0)]
        [DefaultFromSetting("CommandSplatRefactor.VariableName", Default = "$null")]
        [ValidateNotNullOrEmpty]
        public string VariableName { get; set; }

        [Parameter]
        [DefaultFromSetting("CommandSplatRefactor.NoNewLineAfterHashtable", Default = "$false")]
        public SwitchParameter NoNewLineAfterHashtable { get; set; }

        [Parameter]
        [DefaultFromSetting("CommandSplatRefactor.AdditionalParameters", Default = "\'None\'")]
        public AdditionalParameterTypes AdditionalParameters { get; set; }

        [Parameter]
        [DefaultFromSetting("CommandSplatRefactor.ExcludeHints", Default = "$false")]
        public SwitchParameter ExcludeHints { get; set; }

        [Parameter]
        [DefaultFromSetting("CommandSplatRefactor.VariableCaseType", Default = "\'CamelCase\'")]
        public CaseType CaseType { get; set; }
    }
}
