using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    public interface INavigationSupportsOpenDocument
    {
        void OpenDocument(string path);

        void OpenDocument(string path, CancellationToken cancellationToken);

        Task OpenDocumentAsync(string path);

        Task OpenDocumentAsync(string path, CancellationToken cancellationToken);
    }
}
