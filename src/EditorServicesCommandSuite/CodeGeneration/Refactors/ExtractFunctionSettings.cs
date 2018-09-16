using System.Management.Automation;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class ExtractFunctionSettings : RefactorConfiguration
    {
        [Parameter]
        public string FunctionName { get; set; }

        [Parameter]
        public ExtractFunctionDestination Type { get; set; }

        [Parameter]
        public string FilePath { get; set; }
    }
}
