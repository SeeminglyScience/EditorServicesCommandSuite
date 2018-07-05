using System.Collections.Generic;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class RefactorInfo<TTarget> : IRefactorInfo
        where TTarget : class
    {
        internal RefactorInfo(IDocumentRefactorProvider provider, DocumentContextBase request, TTarget target)
        {
            Request = request;
            Provider = provider;
            Target = target;
        }

        public IDocumentRefactorProvider Provider { get; }

        public string Name => Provider.Name;

        public string Description => Provider.Description;

        internal DocumentContextBase Request { get; }

        internal TTarget Target { get; }

        public async Task<IEnumerable<DocumentEdit>> GetDocumentEdits()
        {
            return await Provider.RequestEdits(Request);
        }
    }
}
