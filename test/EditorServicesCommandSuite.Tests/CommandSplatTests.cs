using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Language;
using Xunit;

namespace EditorServicesCommandSuite.Tests
{
    public class CommandSplatTests
    {
        [Fact]
        public async void DoesNothingWithNoParameters()
        {
            Assert.Equal("Get-ChildItem", await GetRefactoredTextAsync("Get-ChildItem"));
        }

        [Fact]
        public async void HandlesSwitchParameters()
        {
            Assert.Equal(
                "$splat = @{\n\tRecurse = $true\n}\nGet-ChildItem @splat",
                await GetRefactoredTextAsync("Get-ChildItem -Recurse"));
        }

        [Fact]
        public async void HandlesQuoting()
        {
            Assert.Equal(
                "$splat = @{\n\tPath = './myPath'\n}\nGet-ChildItem @splat",
                await GetRefactoredTextAsync("Get-ChildItem -Path ./myPath"));
        }

        [Fact]
        public async void HandlesQuotingWithExpressions()
        {
            Assert.Equal(
                "$splat = @{\n\tPath = \"./myPath$c\"\n}\nGet-ChildItem @splat",
                await GetRefactoredTextAsync("Get-ChildItem -Path ./myPath$c"));
        }

        [Fact]
        public async void HandlesAllParameterParameter()
        {
            Assert.Equal(
                // Formatting and ordering in this test is subject to change, depending on implementation goals. For now: order should be
                // bound parameters first, then the other parameters in the current parameterset in order of how Get-Command returns them.
                // List retrieved with: (Get-Command Get-ChildItem).ParameterSets.Where({$_.Parameters.Name -contains "Path"}).Parameters.Name
                "$splat = @{\n\tPath = './myPath'\nFilter\nInclude\nExclude\nRecurse\nDepth\nForce\nName\nVerbose\nDebug\nErrorAction\nWarningAction\nInformationAction\nErrorVariable\nWarningVariable\nInformationVariable\nOutVariable\nOutBuffer\nPipelineVariable\nUseTransaction\nAttributes\nDirectory\nFile\nHidden\nReadOnly\nSystem}\nGet-ChildItem @splat",
                await GetRefactoredTextAsync("Get-ChildItem -Path ./myPath",null,null,true));
        }

        private async Task<string> GetRefactoredTextAsync(
            string testString,
            string variableName = "splat",
            bool newLineAfterHashtable = false,
            bool allParameters = false
        )
        {
            return await MockContext.GetRefactoredTextAsync(
                testString,
                context => CommandSplatRefactor.GetEdits(
                    variableName,
                    context.Ast.FindParent<CommandAst>(),
                    newLineAfterHashtable,
                    allParameters));
        }
    }
}
