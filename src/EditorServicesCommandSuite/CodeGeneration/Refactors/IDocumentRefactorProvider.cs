using System.Collections.Generic;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal interface IDocumentRefactorProvider
    {
        string Id { get; }

        string Name { get; }

        string Description { get; }

        RefactorKind Kind { get; }

        Task<IEnumerable<DocumentEdit>> RequestEdits(DocumentContextBase request);

        bool CanRefactorTarget(DocumentContextBase request);

        bool TryGetRefactorInfo(DocumentContextBase request, out IRefactorInfo info);
    }
}
