using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Host;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal class UIService : IRefactorUI
    {
        private readonly PSHost _host;

        internal UIService(PSHost host)
        {
            _host = host;
        }

        public Task<TItem> ShowChoicePromptAsync<TItem>(string caption, string message, TItem[] items)
        {
            return Task.FromResult(ShowChoicePromptImpl(caption, message, items));
        }

        public Task<TItem> ShowChoicePromptAsync<TItem>(string caption, string message, TItem[] items, Func<TItem, string> labelSelector)
        {
            return Task.FromResult(ShowChoicePromptImpl(caption, message, items, labelSelector));
        }

        public Task<TItem> ShowChoicePromptAsync<TItem>(string caption, string message, TItem[] items, Func<TItem, string> labelSelector, Func<TItem, string> helpMessageSelector)
        {
            return Task.FromResult(ShowChoicePromptImpl(caption, message, items, labelSelector, helpMessageSelector));
        }

        public Task ShowErrorMessageAsync(string message, bool waitForResponse)
        {
            return Task.CompletedTask;
        }

        public Task ShowWarningMessageAsync(string message, bool waitForResponse)
        {
            return Task.CompletedTask;
        }

        private TItem ShowChoicePromptImpl<TItem>(
            string caption,
            string message,
            TItem[] items,
            Func<TItem, string> labelSelector = null,
            Func<TItem, string> helpMessageSelector = null)
        {
            return default(TItem);
        }

        private IEnumerable<ChoiceDescription> ToChoices<TItem>(
            TItem[] items,
            Func<TItem, string> labelSelector,
            Func<TItem, string> helpMessageSelector)
        {
            if (labelSelector == null)
            {
                return items.Select(item => new ChoiceDescription(item.ToString()));
            }

            return items.Select(
                item => new ChoiceDescription(
                    labelSelector(item),
                    helpMessageSelector?.Invoke(item) ?? string.Empty));
        }
    }
}
