using System.Collections.Immutable;
using System.Threading.Tasks;

using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    /// <summary>
    /// Provides a specific refactor option.
    /// </summary>
    internal interface IDocumentRefactorProvider
    {
        /// <summary>
        /// Gets the unique identifer.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of the refactor option.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the code actions that this refactor provider supports.
        /// </summary>
        ImmutableArray<CodeAction> SupportedActions { get; }

        /// <summary>
        /// Determines which code actions (if any) are applicable to the
        /// current context and registers them against <see paramref="context" />.
        /// </summary>
        /// <param name="context">The context for the code action request.</param>
        /// <returns>
        /// A task that represents the asynchronous code action request.
        /// </returns>
        Task ComputeCodeActions(DocumentContextBase context);

        /// <summary>
        /// Processes code actions in the context of an invoked command. This method
        /// should implement any logic specific to the cmdletized version of this
        /// refactor provider.
        /// </summary>
        /// <param name="context">The context for the command invocation.</param>
        /// <returns>
        /// A task that represents the asynchronous invocation.
        /// </returns>
        Task Invoke(DocumentContextBase context);
    }
}
