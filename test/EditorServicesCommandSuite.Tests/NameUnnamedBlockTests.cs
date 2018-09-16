using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Language;
using Xunit;

namespace EditorServicesCommandSuite.Tests
{
    public class NameUnnamedBlockTests
    {
        [Fact]
        public async void NamesUnnamedBlock()
        {
            Assert.Equal(
                "end {\n\tGet-ChildItem\n\tGet-Acl\n}",
                await GetRefactoredTextAsync("Get-ChildItem\nGet-Ac{{c}}l"));
        }

        // [Fact(Skip = "Need to fix test")]
        [Fact]
        public async void DoesNotBreakFunctionSyntax()
        {
            Assert.Equal(
                "function Test {\n\tend {\n\t\tGet-ChildItem\n\t\tGet-Acl\n\t}\n}",
                await GetRefactoredTextAsync("function Test {\n\tGet-ChildItem\n\tGet-Ac{{c}}l\n}"));
        }

        [Fact]
        public async void RetainsIndent()
        {
            Assert.Equal(
                "\tend {\n\t\tGet-ChildItem\n\t\tGet-Acl\n\t}",
                await GetRefactoredTextAsync("\tGet-ChildItem\n\tGet-Acl"));
        }

        private async Task<string> GetRefactoredTextAsync(string testString)
        {
            return await MockContext.GetRefactoredTextAsync(
                testString,
                context => NameUnnamedBlockRefactor.GetEdits(
                    context.Ast.FindParent<NamedBlockAst>(maxDepth: int.MaxValue)));
        }
    }
}
