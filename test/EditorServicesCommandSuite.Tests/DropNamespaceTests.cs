using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using Xunit;

namespace EditorServicesCommandSuite.Tests
{
    public class DropNamespaceTests
    {
        [Fact]
        public async void CanDropNamespace()
        {
            Assert.Equal(
                "using namespace System.IO\n\n[Path]",
                await GetRefactoredTextAsync("[System.IO.Path]"));
        }

        [Fact]
        public async void CanResolveType()
        {
            Assert.Equal(
                "using namespace System.Management.Automation.Host\n\n[PSHost]",
                await GetRefactoredTextAsync("[PSHost]"));
        }

        private async Task<string> GetRefactoredTextAsync(string testString)
        {
            return await MockContext.GetRefactoredTextAsync(
                testString,
                context => new DropNamespaceRefactor().RequestEdits(context, context.Ast));
        }
    }
}
