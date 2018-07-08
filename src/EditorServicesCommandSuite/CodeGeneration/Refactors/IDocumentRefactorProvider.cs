using System.Collections.Generic;
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
        /// Gets the type of object the option can target.
        /// </summary>
        RefactorKind Kind { get; }

        /// <summary>
        /// Requests edits from the refactor provider based on document context.
        /// </summary>
        /// <param name="request">The context of the current document.</param>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain the requested edits.
        /// </returns>
        Task<IEnumerable<DocumentEdit>> RequestEdits(DocumentContextBase request);

        /// <summary>
        /// Determines if the provider is applicable.
        /// </summary>
        /// <param name="request">The context of the current document.</param>
        /// <returns>
        /// A value indicating whether the provider is applicable.
        /// </returns>
        bool CanRefactorTarget(DocumentContextBase request);

        /// <summary>
        /// Attempts to obtain context specific refactor information.
        /// </summary>
        /// <param name="request">The context of the current document.</param>
        /// <param name="info">The context specific refactor information if successful.</param>
        /// <returns>
        /// A value indicating whether refactor information was able to be obtained.
        /// </returns>
        bool TryGetRefactorInfo(DocumentContextBase request, out IRefactorInfo info);
    }
}
