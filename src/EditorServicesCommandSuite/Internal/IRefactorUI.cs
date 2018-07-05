using System;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    public interface IRefactorUI
    {
        Task ShowWarningMessageAsync(string message, bool waitForResponse);

        Task ShowErrorMessageAsync(string message, bool waitForResponse);

        Task<TItem> ShowChoicePromptAsync<TItem>(
            string caption,
            string message,
            TItem[] items);

        Task<TItem> ShowChoicePromptAsync<TItem>(
            string caption,
            string message,
            TItem[] items,
            Func<TItem, string> labelSelector);

        Task<TItem> ShowChoicePromptAsync<TItem>(
            string caption,
            string message,
            TItem[] items,
            Func<TItem, string> labelSelector,
            Func<TItem, string> helpMessageSelector);
    }
}
