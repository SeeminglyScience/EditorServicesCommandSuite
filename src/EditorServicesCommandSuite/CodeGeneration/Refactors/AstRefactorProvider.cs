using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal abstract class AstRefactorProvider<TAst> : IDocumentRefactorProvider
        where TAst : Ast
    {
        public string Id => this.GetType().Name;

        public virtual string Name => this.GetType().Name;

        public virtual string Description => string.Empty;

        public RefactorKind Kind => RefactorKind.Ast;

        bool IDocumentRefactorProvider.CanRefactorTarget(DocumentContextBase request)
        {
            return request.Ast is TAst targetAst && CanRefactorTarget(request, targetAst);
        }

        async Task<IEnumerable<DocumentEdit>> IDocumentRefactorProvider.RequestEdits(DocumentContextBase request)
        {
            if (request.Ast is TAst targetAst)
            {
                return await RequestEdits(request, targetAst);
            }

            if (request.Ast.TryFindParent<TAst>(out TAst targetParent))
            {
                return await RequestEdits(request, targetParent);
            }

            throw new InvalidRefactorTargetException(request);
        }

        bool IDocumentRefactorProvider.TryGetRefactorInfo(DocumentContextBase request, out IRefactorInfo info)
        {
            info = null;
            return
                request.Ast is Ast ast
                    && ast.TryFindParent(out TAst targetAst)
                    && TryGetRefactorInfo(request, targetAst, out info);
        }

        internal abstract Task<IEnumerable<DocumentEdit>> RequestEdits(DocumentContextBase request, TAst ast);

        internal virtual bool CanRefactorTarget(DocumentContextBase request, TAst ast)
        {
            return true;
        }

        internal virtual bool TryGetRefactorInfo(DocumentContextBase request, TAst currentAst, out IRefactorInfo info)
        {
            if (!CanRefactorTarget(request, currentAst))
            {
                info = null;
                return false;
            }

            info = new RefactorInfo<TAst>(this, request, currentAst);
            return true;
        }
    }
}
