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

        private async Task<string> GetRefactoredTextAsync(
            string testString,
            string variableName = "splat",
            bool newLineAfterHashtable = false)
        {
            return await MockContext.GetRefactoredTextAsync(
                testString,
                context => CommandSplatRefactor.GetEdits(
                    variableName,
                    context.Ast.FindParent<CommandAst>(),
                    newLineAfterHashtable));
        }
    }
}
