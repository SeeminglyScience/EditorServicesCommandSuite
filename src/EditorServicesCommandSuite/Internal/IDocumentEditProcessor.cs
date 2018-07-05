using System.Collections.Generic;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    public interface IDocumentEditProcessor
    {
        Task WriteDocumentEditsAsync(IEnumerable<DocumentEdit> edits);
    }
}
