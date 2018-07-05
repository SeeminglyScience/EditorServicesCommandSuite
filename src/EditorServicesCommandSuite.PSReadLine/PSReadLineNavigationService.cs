using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using Microsoft.PowerShell;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal class PSReadLineNavigationService : NavigationService
    {
        public override void SetCursorPosition(int line, int column, CancellationToken cancellationToken)
        {
            SetCursorPositionImpl(
                GetOffsetFromLineAndColumn(line, column));
        }

        public override Task SetCursorPositionAsync(int line, int column, CancellationToken cancellationToken)
        {
            SetCursorPositionImpl(
                GetOffsetFromLineAndColumn(line, column));

            return Task.CompletedTask;
        }

        public override void SetSelection(
            int startLine,
            int startColumn,
            int endLine,
            int endColumn,
            CancellationToken cancellationToken)
        {
            SetCursorPosition(startLine, startColumn, cancellationToken);
        }

        public override Task SetSelectionAsync(
            int startLine,
            int startColumn,
            int endLine,
            int endColumn,
            CancellationToken cancellationToken)
        {
            SetCursorPosition(startLine, startColumn, cancellationToken);
            return Task.CompletedTask;
        }

        private static void SetCursorPositionImpl(int offset)
        {
            PSConsoleReadLine.SetCursorPosition(offset);
        }

        private static int GetOffsetFromLineAndColumn(int line, int column)
        {
            PSConsoleReadLine.GetBufferState(out string input, out _);
            var currentLine = 1;
            var currentColumn = 1;
            return input.TakeWhile(
                c =>
                {
                    if (currentLine < line)
                    {
                        if (c == '\n')
                        {
                            currentLine++;
                        }

                        return true;
                    }

                    if (currentColumn < column)
                    {
                        currentColumn++;
                        return true;
                    }

                    return false;
                })
                .Count();
        }
    }
}
