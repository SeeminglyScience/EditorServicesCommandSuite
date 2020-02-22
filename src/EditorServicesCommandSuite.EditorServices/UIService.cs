using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices.Extensions.Services;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class UIService : IRefactorUI
    {
        private readonly ILanguageServerService _messages;

        private readonly IEditorUIService _ui;

        internal UIService(ILanguageServerService messages, IEditorUIService ui)
        {
            _messages = messages;
            _ui = ui;
        }

        public Task ShowWarningMessageAsync(
            string message,
            bool waitForResponse = false)
        {
            _messages.ShowWarning(message);
            return Task.CompletedTask;
        }

        public Task ShowErrorMessageAsync(
            string message,
            bool waitForResponse = false)
        {
            _messages.ShowError(message);
            return Task.CompletedTask;
        }

        public async Task<string> ShowInputPromptAsync(
            string caption,
            string message,
            bool waitForResponse)
        {
            string combinedMessage = string.IsNullOrEmpty(caption)
                ? message
                : $"{caption} - {message}";

            string response = await _ui.PromptInputAsync(combinedMessage).ConfigureAwait(false);
            if (response == null)
            {
                throw new OperationCanceledException();
            }

            return response;
        }

        public async Task<TItem> ShowChoicePromptAsync<TItem>(
            string caption,
            string message,
            TItem[] items)
        {
            return await ShowChoicePromptAsync(
                caption,
                message,
                items,
                labelSelector: null,
                helpMessageSelector: null)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task<TItem> ShowChoicePromptAsync<TItem>(
            string caption,
            string message,
            TItem[] items,
            Func<TItem, string> labelSelector)
        {
            return await ShowChoicePromptAsync(
                caption,
                message,
                items,
                labelSelector,
                helpMessageSelector: null)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task<TItem> ShowChoicePromptAsync<TItem>(
            string caption,
            string message,
            TItem[] items,
            Func<TItem, string> labelSelector,
            Func<TItem, string> helpMessageSelector)
        {
            string combinedMessage = string.IsNullOrEmpty(caption)
                ? message
                : $"{caption} - {message}";

            var response = await _ui.PromptSelectionAsync(
                combinedMessage,
                GetChoiceDetails(items, labelSelector, helpMessageSelector))
                .ConfigureAwait(false);

            if (response == null)
            {
                throw new OperationCanceledException();
            }

            return items[GetIndexFromString(response)];
        }

        private int GetIndexFromString(string response)
        {
            return int.Parse(
                response.Substring(
                    0,
                    response.IndexOf(Symbols.Space) + 1)) - 1;
        }

        private IReadOnlyList<PromptChoiceDetails> GetChoiceDetails<TItem>(
            TItem[] items,
            Func<TItem, string> labelSelector,
            Func<TItem, string> helpMessageSelector)
        {
            if (labelSelector == null)
            {
                labelSelector = item => item.ToString();
            }

            var choices = new PromptChoiceDetails[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                TItem currentItem = items[i];
                string label = string.Format(
                    CultureInfo.CurrentCulture,
                    "{0} - {1}",
                    (i + 1).ToString(),
                    labelSelector(currentItem));

                // Prompt choice details constructor throws when there is a comma in the
                // label for right now. Currently we replace that with a "full width comma"
                // (0xFF0C).
                label = label.Replace(',', '\xFF0C');

                string helpMessage = helpMessageSelector?.Invoke(currentItem) ?? string.Empty;

                choices[i] = new PromptChoiceDetails(label, helpMessage);
            }

            return choices;
        }

        private class ShowInputPromptRequestArgs
        {
            public string Name { get; set; }

            public string Label { get; set; }
        }

        private class ShowInputPromptResponseBody
        {
            public string ResponseText { get; set; }

            public bool PromptCancelled { get; set; }
        }
    }
}
