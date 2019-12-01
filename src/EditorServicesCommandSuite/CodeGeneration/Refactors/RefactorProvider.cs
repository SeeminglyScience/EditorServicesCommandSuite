using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal abstract class RefactorProvider : IDocumentRefactorProvider
    {
        protected static readonly ImmutableArray<CodeAction> EmptyCodeActions = ImmutableArray<CodeAction>.Empty;

        public virtual string Id => GetType().Name;

        public virtual string Name => GetType().Name;

        public virtual string Description => string.Empty;

        public abstract ImmutableArray<CodeAction> SupportedActions { get; }

        public abstract Task ComputeCodeActions(DocumentContextBase context);

        public virtual async Task Invoke(DocumentContextBase context)
        {
            await ComputeCodeActions(context).ConfigureAwait(false);
            CodeAction[] codeActions = await context.FinalizeCodeActions().ConfigureAwait(false);
            if (codeActions == null || codeActions.Length == 0)
            {
                return;
            }

            CodeAction selectedAction = null;
            if (codeActions.Length == 1)
            {
                selectedAction = codeActions[0];
            }
            else
            {
                IRefactorUI ui = CommandSuite.Instance.UI;
                if (CommandSuite.Instance.UI == null)
                {
                    throw new InvalidOperationException(
                        "This action requires a supported UI to be registered.");
                }

                selectedAction = await CommandSuite.Instance.UI.ShowChoicePromptAsync(
                    RefactorStrings.SelectRefactorCaption,
                    RefactorStrings.SelectRefactorMessage,
                    codeActions,
                    item => item.Title)
                    .ConfigureAwait(false);
            }

            if (selectedAction == null)
            {
                return;
            }

            await ProcessActionForInvoke(context, selectedAction).ConfigureAwait(false);
        }

        protected async Task ProcessActionForInvoke(DocumentContextBase context, CodeAction action)
        {
            await action.ComputeChanges(context).ConfigureAwait(false);
            WorkspaceChange[] changes = await context.FinalizeWorkspaceChanges().ConfigureAwait(false);
            await CommandSuite.Instance
                .ProcessWorkspaceChanges(changes, context.CancellationToken)
                .ConfigureAwait(false);
        }
    }
}
