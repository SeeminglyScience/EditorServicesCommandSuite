using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal abstract class SelectionRefactor : IDocumentRefactorProvider
    {
        public string Id => this.GetType().Name;

        public virtual string Name => this.GetType().Name;

        public virtual string Description => string.Empty;

        public RefactorKind Kind => RefactorKind.Selection;

        bool IDocumentRefactorProvider.CanRefactorTarget(DocumentContextBase request)
        {
            return CanRefactorTarget(request, request.SelectionExtent);
        }

        async Task<IEnumerable<DocumentEdit>> IDocumentRefactorProvider.RequestEdits(DocumentContextBase request)
        {
            return await RequestEdits(request, request.SelectionExtent);
        }

        bool IDocumentRefactorProvider.TryGetRefactorInfo(
            DocumentContextBase request,
            out IRefactorInfo info)
        {
            info = null;
            return CanRefactorTarget(request, request.SelectionExtent)
                && TryGetRefactorInfo(request, request.SelectionExtent, out info);
        }

        internal virtual bool CanRefactorTarget(DocumentContextBase request, IScriptExtent extent)
        {
            return extent.StartOffset != extent.EndOffset;
        }

        internal virtual bool TryGetRefactorInfo(DocumentContextBase request, IScriptExtent extent, out IRefactorInfo info)
        {
            info = new RefactorInfo<IScriptExtent>(this, request, extent);
            return true;
        }

        internal abstract Task<IEnumerable<DocumentEdit>> RequestEdits(DocumentContextBase request, IScriptExtent extent);
    }
}
