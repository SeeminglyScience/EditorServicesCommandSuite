using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Internal
{
    public interface IRefactorNavigation
    {
        void SetCursorPosition(int line, int column);

        void SetCursorPosition(int line, int column, CancellationToken cancellationToken);

        void SetCursorPosition(IScriptPosition position);

        void SetCursorPosition(IScriptPosition position, CancellationToken cancellationToken);

        Task SetCursorPositionAsync(int line, int column);

        Task SetCursorPositionAsync(int line, int column, CancellationToken cancellationToken);

        Task SetCursorPositionAsync(IScriptPosition position);

        Task SetCursorPositionAsync(IScriptPosition position, CancellationToken cancellationToken);

        void SetSelection(int startLine, int startColumn, int endLine, int endColumn);

        void SetSelection(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken);

        void SetSelection(IScriptExtent extent);

        void SetSelection(IScriptExtent extent, CancellationToken cancellationToken);

        Task SetSelectionAsync(int startLine, int startColumn, int endLine, int endColumn);

        Task SetSelectionAsync(int startLine, int startColumn, int endLine, int endColumn, CancellationToken cancellationToken);

        Task SetSelectionAsync(IScriptExtent extent);

        Task SetSelectionAsync(IScriptExtent extent, CancellationToken cancellationToken);
    }
}
