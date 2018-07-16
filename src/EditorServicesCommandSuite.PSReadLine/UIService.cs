using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal class UIService : IRefactorUI
    {
        private readonly ConcurrentQueue<Message> s_messages = new ConcurrentQueue<Message>();

        private enum MessageType
        {
            Information,

            Warning,

            Error,
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
            WriteOrQueueMessage(MessageType.Error, message);
            return Task.CompletedTask;
        }

        public Task ShowWarningMessageAsync(string message, bool waitForResponse)
        {
            WriteOrQueueMessage(MessageType.Warning, message);
            return Task.CompletedTask;
        }

        private void WriteOrQueueMessage(MessageType type, string content)
        {
            if (Menus.IsInAlternateBuffer)
            {
                s_messages.Enqueue(
                    new Message()
                    {
                        Type = type,
                        Content = content,
                    });
                return;
            }

            WriteMessageImpl(type, content);
        }

        private void WriteMessageImpl(Message message)
        {
            WriteMessageImpl(message.Type, message.Content);
        }

        private void WriteMessageImpl(MessageType type, string content)
        {
            // Replace ASAP with writing to CommandRuntime once a thread controller is written.
            Console.WriteLine();
            switch (type)
            {
                case MessageType.Error: Console.Write(Ansi.Colors.Error); break;
                case MessageType.Warning: Console.Write(Ansi.Colors.Warning); break;
                case MessageType.Information: Console.Write(Ansi.Colors.Information); break;
            }

            Console.WriteLine(content);
        }

        private TItem ShowChoicePromptImpl<TItem>(
            string caption,
            string message,
            TItem[] items,
            Func<TItem, string> labelSelector = null,
            Func<TItem, string> helpMessageSelector = null)
        {
            PSConsoleReadLine.GetBufferState(out string input, out int cursor);

            TItem result;
            using (SelectItemMenu<TItem> itemSelect = Menus.ItemSelect(caption, message, items))
            {
                if (labelSelector != null)
                {
                    itemSelect.RenderItem((index, item) => labelSelector(item));
                }

                if (helpMessageSelector != null)
                {
                    itemSelect.RenderItemDescription((index, item) => helpMessageSelector(item));
                }

                itemSelect.CompletionData =
                    CommandCompletion.MapStringInputToParsedInput(
                        input,
                        cursor);

                result = itemSelect.Bind();
            }

            while (s_messages.TryDequeue(out Message queuedMessage))
            {
                WriteMessageImpl(queuedMessage);
            }

            return result;
        }

        private struct Message
        {
            internal MessageType Type;

            internal string Content;
        }
    }
}
