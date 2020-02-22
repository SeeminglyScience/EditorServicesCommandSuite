using System.Collections.Immutable;
using System.Management.Automation.Language;
using System.Threading.Tasks;

using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class ChangeNamedBlockKindRefactor : RefactorProvider
    {
        public override ImmutableArray<CodeAction> SupportedActions { get; }
            = ImmutableArray.Create(
                CodeAction.Inactive(
                    CodeActionIds.ChangeNamedBlockKind,
                    "Change block kind to '{0}'"));

        public override async Task ComputeCodeActions(DocumentContextBase context)
        {
            if (context.Token.Kind == TokenKind.Begin)
            {
                await context.RegisterCodeActionAsync(CreateCodeAction(TokenKind.Process)).ConfigureAwait(false);
                await context.RegisterCodeActionAsync(CreateCodeAction(TokenKind.End)).ConfigureAwait(false);
                return;
            }

            if (context.Token.Kind == TokenKind.Process)
            {
                await context.RegisterCodeActionAsync(CreateCodeAction(TokenKind.Begin)).ConfigureAwait(false);
                await context.RegisterCodeActionAsync(CreateCodeAction(TokenKind.End)).ConfigureAwait(false);
                return;
            }

            if (context.Token.Kind == TokenKind.End)
            {
                await context.RegisterCodeActionAsync(CreateCodeAction(TokenKind.Begin)).ConfigureAwait(false);
                await context.RegisterCodeActionAsync(CreateCodeAction(TokenKind.Process)).ConfigureAwait(false);
                return;
            }
        }

        private static Task ChangeBlockKindAsync(DocumentContextBase context, TokenKind kind)
        {
            var writer = new PowerShellScriptWriter(context);
            writer.StartWriting(context.Token);
            writer.Write(kind);
            return writer.RegisterWorkspaceChangeAsync(context);
        }

        private CodeAction CreateCodeAction(TokenKind kind)
        {
            CodeAction source = SupportedActions[0];
            return source.With(
                (context) => ChangeBlockKindAsync(context, kind),
                title: string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    source.Title,
                    TokenTraits.Text(kind)));
        }
    }
}
