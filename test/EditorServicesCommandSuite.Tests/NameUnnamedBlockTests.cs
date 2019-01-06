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
                TestBuilder.Create()
                    .Lines("end {")
                    .Lines("    Get-ChildItem")
                    .Lines("    Get-Acl")
                    .Texts("}"),
                await GetRefactoredTextAsync(
                    TestBuilder.Create()
                        .Lines("Get-ChildItem")
                        .Texts("Get-Ac{0}l", hasCursor: true)));
        }

        [Fact(Skip = "Need to fix test")]
        public async void DoesNotBreakFunctionSyntax()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("function Test {")
                    .Lines("    end {")
                    .Lines("        Get-ChildItem")
                    .Lines("        Get-Acl")
                    .Lines("    }")
                    .Texts("}"),
                await GetRefactoredTextAsync(
                    TestBuilder.Create()
                        .Lines("function Test {")
                        .Lines("    Get-ChildItem")
                        .Lines("    Get-Ac{0}l", hasCursor: true)
                        .Texts("}")));
        }

        [Fact]
        public async void RetainsIndent()
        {
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("    end {")
                    .Lines("        Get-ChildItem")
                    .Lines("        Get-Acl")
                    .Texts("    }"),
                await GetRefactoredTextAsync(
                    TestBuilder.Create()
                        .Lines("    Get-ChildItem")
                        .Texts("    Get-Acl")));
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
