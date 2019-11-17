using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell.EditorServices.Services.PowerShellContext;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class UIService : IRefactorUI
    {
        private readonly MessageService _messages;

        internal UIService(MessageService messages)
        {
            _messages = messages;
        }

        public async Task ShowWarningMessageAsync(
            string message,
            bool waitForResponse = false)
        {
            await _messages.SendRequestAsync(
                Messages.ShowWarningMessage,
                message)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task ShowErrorMessageAsync(
            string message,
            bool waitForResponse = false)
        {
            await _messages.SendRequestAsync(
                Messages.ShowErrorMessage,
                message)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task<string> ShowInputPromptAsync(
            string caption,
            string message,
            bool waitForResponse)
        {
            ShowInputPromptResponse response = await _messages.SendRequestAsync(
                Messages.ShowInputPrompt,
                new ShowInputPromptRequest()
                {
                    Name = caption,
                    Label = message,
                })
                .ConfigureAwait(continueOnCapturedContext: false);

            if (response == null || response.PromptCancelled)
            {
                throw new OperationCanceledException();
            }

            return response.ResponseText;
        }

        public async Task<TItem> ShowChoicePromptAsync<TItem>(
            string caption,
            string message,
            TItem[] items)
        {
            return await ShowChoicePromptAsync<TItem>(
                caption,
                message,
                items,
                null,
                null)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task<TItem> ShowChoicePromptAsync<TItem>(
            string caption,
            string message,
            TItem[] items,
            Func<TItem, string> labelSelector)
        {
            return await ShowChoicePromptAsync<TItem>(
                caption,
                message,
                items,
                labelSelector,
                null)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task<TItem> ShowChoicePromptAsync<TItem>(
            string caption,
            string message,
            TItem[] items,
            Func<TItem, string> labelSelector,
            Func<TItem, string> helpMessageSelector)
        {
            var response = await _messages.SendRequestAsync(
                Messages.ShowChoicePrompt,
                new ShowChoicePromptRequest()
                {
                    IsMultiChoice = false,
                    Caption = caption,
                    Message = message,
                    Choices = GetChoiceDetails(
                        items,
                        labelSelector,
                        helpMessageSelector)
                        .ToArray(),
                })
                .ConfigureAwait(continueOnCapturedContext: false);

            if (response == null || response.PromptCancelled)
            {
                throw new OperationCanceledException();
            }

            return items[GetIndexFromString(response.ResponseText)];
        }

        private int GetIndexFromString(string response)
        {
            return int.Parse(
                response.Substring(
                    0,
                    response.IndexOf(Symbols.Space) + 1)) - 1;
        }

        private IEnumerable<ChoiceDetails> GetChoiceDetails<TItem>(
            TItem[] items,
            Func<TItem, string> labelSelector,
            Func<TItem, string> helpMessageSelector)
        {
            if (labelSelector == null)
            {
                labelSelector = item => item.ToString();
            }

            for (var i = 1; i <= items.Length; i++)
            {
                yield return new ChoiceDetails()
                {
                    Label =
                        i.ToString()
                        + Symbols.Space
                        + Symbols.Dash
                        + Symbols.Space
                        + labelSelector(items[i - 1]),
                    HelpMessage =
                        helpMessageSelector == null
                            ? string.Empty
                            : helpMessageSelector(items[i - 1]),
                };
            }
        }
    }
}
