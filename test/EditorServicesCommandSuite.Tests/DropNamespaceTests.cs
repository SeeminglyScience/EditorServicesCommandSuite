using System;
using System.Linq;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using Xunit;

namespace EditorServicesCommandSuite.Tests
{
    public class DropNamespaceTests
    {
        private readonly MockedRefactorService _refactorService = new MockedRefactorService(new DropNamespaceRefactor());

        [Fact]
        public async void CanDropNamespace()
        {
            using var mock = await _refactorService.CreateContextAsync("[System.IO.Path]");
            var codeActions = await mock.GetCodeActionsAsync();

            Assert.NotEmpty(codeActions);
            Assert.Single(codeActions);
            Assert.Equal(CodeActionIds.AlterTypeExpression, codeActions[0].Id);

            await codeActions[0].ComputeChanges(mock.Context);
            WorkspaceChange[] changes = await mock.Context.FinalizeWorkspaceChanges();
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("using namespace System.IO")
                    .Lines()
                    .Texts("[Path]"),
                mock.ProcessChanges(changes));
        }

        [Fact]
        public async void CanResolveType()
        {
            using var mock = await _refactorService.CreateContextAsync("[Path]", requiresRunspace: true);
            var codeActions = await mock.GetCodeActionsAsync();
            var resolveAction = Array.Find(
                codeActions,
                action => action.Id == CodeActionIds.AlterTypeExpression);

            Assert.NotNull(resolveAction);

            await resolveAction?.ComputeChanges(mock.Context);
            WorkspaceChange[] changes = await mock.Context.FinalizeWorkspaceChanges();
            Assert.Equal(
                TestBuilder.Create()
                    .Lines("using namespace System.IO")
                    .Lines()
                    .Texts("[Path]"),
                mock.ProcessChanges(changes));
        }
    }
}
