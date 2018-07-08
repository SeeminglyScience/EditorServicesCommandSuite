using System;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Provides the ability to interact with the UI of the host editor.
    /// </summary>
    public interface IRefactorUI
    {
        /// <summary>
        /// Shows a warning message.
        /// </summary>
        /// <param name="message">The message to show.</param>
        /// <param name="waitForResponse">
        /// A value indicating whether the method should complete after the editor host responds.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        Task ShowWarningMessageAsync(string message, bool waitForResponse);

        /// <summary>
        /// Shows an error message.
        /// </summary>
        /// <param name="message">The message to show.</param>
        /// <param name="waitForResponse">
        /// A value indicating whether the method should complete after the editor host responds.
        /// </param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation.
        /// </returns>
        Task ShowErrorMessageAsync(string message, bool waitForResponse);

        /// <summary>
        /// Uses the editor host UI to prompt the user to choose an item from a list.
        /// </summary>
        /// <param name="caption">The caption for the prompt.</param>
        /// <param name="message">The message for the prompt.</param>
        /// <param name="items">The items to present as choices.</param>
        /// <typeparam name="TItem">The type of item that makes up the choices.</typeparam>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain the chosen item.
        /// </returns>
        Task<TItem> ShowChoicePromptAsync<TItem>(
            string caption,
            string message,
            TItem[] items);

        /// <summary>
        /// Uses the editor host UI to prompt the user to choose an item from a list.
        /// </summary>
        /// <param name="caption">The caption for the prompt.</param>
        /// <param name="message">The message for the prompt.</param>
        /// <param name="items">The items to present as choices.</param>
        /// <param name="labelSelector">
        /// A method that will be invoked to determine the label for each item.
        /// </param>
        /// <typeparam name="TItem">The type of item that makes up the choices.</typeparam>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain the chosen item.
        /// </returns>
        Task<TItem> ShowChoicePromptAsync<TItem>(
            string caption,
            string message,
            TItem[] items,
            Func<TItem, string> labelSelector);

        /// <summary>
        /// Uses the editor host UI to prompt the user to choose an item from a list.
        /// </summary>
        /// <param name="caption">The caption for the prompt.</param>
        /// <param name="message">The message for the prompt.</param>
        /// <param name="items">The items to present as choices.</param>
        /// <param name="labelSelector">
        /// A method that will be invoked to determine the label for each item.
        /// </param>
        /// <param name="helpMessageSelector">
        /// A method that will be invoked to determine the help message for each item.
        /// </param>
        /// <typeparam name="TItem">The type of item that makes up the choices.</typeparam>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain the chosen item.
        /// </returns>
        Task<TItem> ShowChoicePromptAsync<TItem>(
            string caption,
            string message,
            TItem[] items,
            Func<TItem, string> labelSelector,
            Func<TItem, string> helpMessageSelector);
    }
}
