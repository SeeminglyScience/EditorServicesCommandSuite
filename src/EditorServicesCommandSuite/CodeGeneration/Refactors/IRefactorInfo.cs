using System.Collections.Generic;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    /// <summary>
    /// Provides context specific information for a refactor request.
    /// </summary>
    internal interface IRefactorInfo
    {
        /// <summary>
        /// Gets the name of the refactor provider.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the description of the refactor option.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the refactor provider.
        /// </summary>
        IDocumentRefactorProvider Provider { get; }

        /// <summary>
        /// Requests edits from the refactor provider based the
        /// context contained in the refactor information.
        /// </summary>
        /// <returns>
        /// A <see cref="Task" /> object representing the asynchronus operation. The Result property
        /// will contain the requested edits.
        /// </returns>
        Task<IEnumerable<DocumentEdit>> GetDocumentEdits();
    }
}
