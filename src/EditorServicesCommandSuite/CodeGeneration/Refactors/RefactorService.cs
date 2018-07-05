using System.Collections.Generic;
using System.Linq;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class RefactorService
    {
        private readonly Dictionary<string, IDocumentRefactorProvider> _providers =
            new Dictionary<string, IDocumentRefactorProvider>();

        internal void RegisterProvider(IDocumentRefactorProvider provider)
        {
            Validate.IsNotNull(nameof(provider), provider);
            _providers.Add(provider.Id, provider);
        }

        internal void UnregisterProvider(IDocumentRefactorProvider provider)
        {
            Validate.IsNotNull(nameof(provider), provider);
            _providers.Remove(provider.Name);
        }

        internal IEnumerable<IRefactorInfo> GetRefactorOptions(DocumentContextBase request)
        {
            foreach (var provider in _providers.Values.OrderBy(p => p.Kind))
            {
                if (provider.TryGetRefactorInfo(request, out IRefactorInfo info))
                {
                    yield return info;
                }
            }
        }

        internal IDocumentRefactorProvider GetProvider(string typeName)
        {
            if (_providers.TryGetValue(typeName, out IDocumentRefactorProvider result))
            {
                return result;
            }

            return null;
        }
    }
}
