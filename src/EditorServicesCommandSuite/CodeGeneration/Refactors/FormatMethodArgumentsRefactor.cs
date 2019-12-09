using System.Collections.Immutable;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;

using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsCommon.Set, "ArgumentListFormat")]
    internal sealed class FormatMethodArgumentsRefactor : RefactorProvider
    {
        public override ImmutableArray<CodeAction> SupportedActions { get; } =
            ImmutableArray.Create(
                CodeAction.Inactive(
                    CodeActionIds.FormatMethodArguments,
                    FormatMethodArgumentsStrings.FormatSeparateLines),
                CodeAction.Inactive(
                    CodeActionIds.FormatMethodArguments,
                    FormatMethodArgumentsStrings.FormatSingleLine));

        private CodeAction SeparateLinesAction => SupportedActions[0];

        private CodeAction SingleLineAction => SupportedActions[1];

        public override async Task ComputeCodeActions(DocumentContextBase context)
        {
            if (!context.Ast.TryFindParent(maxDepth: 3, out InvokeMemberExpressionAst invokeMember))
            {
                return;
            }

            if (invokeMember.Arguments.Count <= 1)
            {
                return;
            }

            bool allSameLine = true;
            int syncLine = invokeMember.Member.Extent.EndLineNumber;
            foreach (ExpressionAst argument in invokeMember.Arguments)
            {
                if (argument.Extent.StartLineNumber != syncLine)
                {
                    allSameLine = false;
                    break;
                }
            }

            if (allSameLine)
            {
                await context.RegisterCodeActionAsync(
                    SeparateLinesAction.With(FormatSeparateLines, invokeMember))
                    .ConfigureAwait(false);
                return;
            }

            await context.RegisterCodeActionAsync(
                SingleLineAction.With(FormatSingleLine, invokeMember))
                .ConfigureAwait(false);
        }

        private static async Task FormatSingleLine(
            DocumentContextBase context,
            InvokeMemberExpressionAst invokeMember)
        {
            var writer = new PowerShellScriptWriter(invokeMember);
            TokenNode lparenToken = context.Token.List.First.FindNextOrSelf()
                .ContainingOrAfterEndOf(invokeMember.Member)
                .IncludeSelf().OfKind(TokenKind.LParen)
                .GetResult();

            TokenFinder finder = lparenToken.FindNext().OnlyWithin(invokeMember);
            bool isFirstNewLine = true;
            while (finder.OfKind(TokenKind.NewLine).TryGetResult(out TokenNode newLineNode))
            {
                bool isInArgument = false;
                foreach (ExpressionAst argument in invokeMember.Arguments)
                {
                    if (argument.Extent.ContainsExtent(newLineNode.Value.Extent))
                    {
                        isInArgument = true;
                        break;
                    }
                }

                if (isInArgument)
                {
                    continue;
                }

                if (newLineNode.Value.Extent.IsAfter(invokeMember.Extent))
                {
                    break;
                }

                if (!newLineNode.TryGetNext(out TokenNode nodeAfterNewLine))
                {
                    continue;
                }

                writer.StartWriting(
                    newLineNode.Value.Extent.StartOffset,
                    nodeAfterNewLine.Value.Extent.StartOffset);

                if (isFirstNewLine)
                {
                    writer.Write(string.Empty);
                    isFirstNewLine = false;
                }
                else
                {
                    writer.Write(Symbols.Space);
                }

                writer.FinishWriting();
            }

            await context.RegisterWorkspaceChangeAsync(
                WorkspaceChange.EditDocument(context.Document, writer.Edits))
                .ConfigureAwait(false);
        }

        private static async Task FormatSeparateLines(
            DocumentContextBase context,
            InvokeMemberExpressionAst invokeMember)
        {
            TokenNode lparenToken = context.Token.List.First.FindNext()
                .ContainingOrAfterEndOf(invokeMember.Member)
                .IncludeSelf().OfKind(TokenKind.LParen)
                .GetResult();

            var writer = new PowerShellScriptWriter(context);
            int lastLine = lparenToken.Value.Extent.EndLineNumber;
            int argCount = invokeMember.Arguments.Count;
            for (int i = 0; i < argCount; i++)
            {
                ExpressionAst arg = invokeMember.Arguments[i];
                if (arg.Extent.StartLineNumber == lastLine)
                {
                    writer.StartWriting(arg.Extent.StartOffset);
                    writer.PushIndent();
                    writer.WriteLine();
                    writer.WriteIndentIfPending();
                    writer.FinishWriting();

                    if (i == argCount - 1)
                    {
                        // Shouldn't be anything to trim if this is the last argument.
                        continue;
                    }

                    TokenNode separator = lparenToken
                        .FindNext()
                        .OnlyWithin(invokeMember)
                        .ContainingEndOf(arg)
                        .Then().IncludeSelf().OfKind(TokenKind.Comma)
                        .GetResult();

                    Token tokenAfter = separator.Next.Value;
                    if (separator.Value.Extent.StartLineNumber != tokenAfter.Extent.StartLineNumber)
                    {
                        // Don't try to trim line if there was already a line break between this
                        // argument and the next.
                        continue;
                    }

                    int positionDifference = tokenAfter.Extent.StartOffset - separator.Value.Extent.EndOffset;
                    if (positionDifference == 0)
                    {
                        // Nothing to trim if the separator is right next to the next token.
                        continue;
                    }

                    writer.StartWriting(
                        separator.Value.Extent.EndOffset,
                        tokenAfter.Extent.StartOffset);

                    writer.Write(string.Empty);
                    writer.FinishWriting();
                }

                lastLine = arg.Extent.EndLineNumber;
            }

            await context.RegisterWorkspaceChangeAsync(
                WorkspaceChange.EditDocument(context.Document, writer.Edits))
                .ConfigureAwait(false);
        }
    }
}
