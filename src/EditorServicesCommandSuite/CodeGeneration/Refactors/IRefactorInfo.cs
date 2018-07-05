using System.Collections.Generic;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal interface IRefactorInfo
    {
        string Name { get; }

        string Description { get; }

        IDocumentRefactorProvider Provider { get; }

        Task<IEnumerable<DocumentEdit>> GetDocumentEdits();
    }
}
