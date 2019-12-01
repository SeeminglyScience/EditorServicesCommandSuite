using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        internal async Task<CodeAction[]> GetCodeActionsAsync(DocumentContextBase context)
        {
            foreach (IDocumentRefactorProvider provider in _providers.Values)
            {
                await provider.ComputeCodeActions(context).ConfigureAwait(false);
            }

            return await context.FinalizeCodeActions().ConfigureAwait(false);
        }

        internal IDocumentRefactorProvider GetProvider(string typeName)
        {
            if (_providers.TryGetValue(typeName, out IDocumentRefactorProvider result))
            {
                return result;
            }

            return null;
        }

        internal IDocumentRefactorProvider[] GetProviders()
        {
            return _providers.Values.ToArray();
        }
    }
}
