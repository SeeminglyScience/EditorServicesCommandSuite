using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal abstract class TokenRefactorProvider<TToken> : IDocumentRefactorProvider
        where TToken : Token
    {
        public string Id => this.GetType().Name;

        public virtual string Name => this.GetType().Name;

        public virtual string Description => string.Empty;

        public RefactorKind Kind => RefactorKind.Token;

        bool IDocumentRefactorProvider.CanRefactorTarget(DocumentContextBase request)
        {
            return request.Token.Value is TToken token
                && CanRefactorToken(request, token);
        }

        async Task<IEnumerable<DocumentEdit>> IDocumentRefactorProvider.RequestEdits(DocumentContextBase request)
        {
            return
                request.Token.Value is TToken token
                    ? await RequestEdits(request, token)
                    : throw new InvalidRefactorTargetException(request);
        }

        bool IDocumentRefactorProvider.TryGetRefactorInfo(DocumentContextBase request, out IRefactorInfo info)
        {
            info = null;
            return request.Token.Value is TToken token &&
                TryGetRefactorInfo(request, token, out info);
        }

        internal abstract Task<IEnumerable<DocumentEdit>> RequestEdits(DocumentContextBase request, TToken token);

        internal virtual bool CanRefactorToken(DocumentContextBase request, TToken token)
        {
            return true;
        }

        internal virtual bool TryGetRefactorInfo(DocumentContextBase request, TToken token, out IRefactorInfo info)
        {
            if (!CanRefactorToken(request, token))
            {
                info = null;
                return false;
            }

            info = new RefactorInfo<TToken>(this, request, token);
            return true;
        }
    }
}
