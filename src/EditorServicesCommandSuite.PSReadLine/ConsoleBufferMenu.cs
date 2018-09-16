using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation.Language;
using System.Text;
using System.Text.RegularExpressions;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal abstract class ConsoleBufferMenu<TResult> : IDisposable
    {
        protected readonly TextWriter _out;

        protected readonly StringBuilder _input = new StringBuilder(Console.WindowWidth - 1);

        protected int _inputLine;

        protected int _cursorX;

        protected int _lastWindowWidth = Console.WindowWidth;

        protected int _lastWindowHeight = Console.WindowHeight;

        private static readonly MatchCollection s_emptyMatchCollection = Regex.Matches(string.Empty, "z");

        private string _renderedScript;

        private bool _isDisposed;

        private bool _isInputAccepted;

        private bool _isHeaderWritten;

        internal ConsoleBufferMenu(string caption, string message)
        {
            Caption = caption;
            Message = message;
            _out = Console.Out;
        }

        internal Tuple<Ast, Token[], IScriptPosition> CompletionData { get; set; }

        internal string Caption { get; }

        internal string Message { get; }

        public void Dispose()
        {
            Dispose(true);
        }

        internal TResult Bind()
        {
            ThrowIfDisposed();
            using (Menus.NewAlternateBuffer())
            {
                return ReadInput();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                DisposeImpl();
            }

            _isDisposed = true;
        }

        protected virtual void DisposeImpl()
        {
        }

        protected virtual void MoveSelectionUp(
            ConsoleKeyInfo key,
            ConsoleModifiers requiredModifiers = default(ConsoleModifiers))
        {
        }

        protected virtual void MoveSelectionDown(
            ConsoleKeyInfo key,
            ConsoleModifiers requiredModifiers = default(ConsoleModifiers))
        {
        }

        protected virtual void AfterInputRender()
        {
        }

        protected virtual void RenderBody(bool isForSelectionChange)
        {
        }

        protected abstract TResult GetResult();

        protected void ThrowIfDisposed()
        {
            if (!_isDisposed)
            {
                return;
            }

            throw new InvalidOperationException();
        }

        protected void Render(bool isForSelectionChange = false)
        {
            if (_lastWindowWidth != Console.WindowWidth ||
                _lastWindowHeight != Console.WindowHeight)
            {
                // Force full redraw on window resize.
                EnsureInputWithinBounds();
                _lastWindowWidth = Console.WindowWidth;
                _lastWindowHeight = Console.WindowHeight;
                isForSelectionChange = false;
                _isHeaderWritten = false;
                SetCursorX(0);
                SetCursorY(0);
                _out.Write(Ansi.ClearScreen);
            }

            if (!_isHeaderWritten)
            {
                WriteSubjectScript();
                _out.Write(Ansi.Colors.Primary);
                _out.WriteLine(Caption);
                _out.Write(Ansi.Colors.Secondary);
                _out.WriteLine(Message);
                _out.Write(Ansi.Modification.InsertLines, 2);
                _inputLine = Console.CursorTop;
                _out.WriteLine(_input);
                AfterInputRender();
                _isHeaderWritten = true;
            }

            RenderBody(isForSelectionChange);
        }

        protected void Write(string buffer, int index, int count, bool newLine = false, string color = null)
        {
            if (string.IsNullOrEmpty(color))
            {
                _out.Write(Ansi.Colors.Default);
            }
            else
            {
                _out.Write(color);
            }

            _out.Write(buffer, index, count);

            if (newLine)
            {
                _out.WriteLine();
            }
        }

        protected void WriteEmphasis(string buffer, int index, int count)
        {
            _out.Write(Ansi.Colors.Emphasis);
            _out.Write(buffer, index, count);
        }

        protected void SetCursorX(int x)
        {
            _out.Write(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Ansi.Movement.SetHorizontalCursorPosition,
                    x + 1));
        }

        protected void SetCursorY(int y)
        {
            _out.Write(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Ansi.Movement.SetVerticalCursorPosition,
                    y));
        }

        protected void ClearLine(int amount = 1)
        {
            if (amount == 1)
            {
                _out.Write(Ansi.ClearCurrentLine);
                return;
            }

            _out.Write(
                string.Format(
                    CultureInfo.InvariantCulture,
                    Ansi.ClearLines,
                    amount));
        }

        protected void SelfInsert(ConsoleKeyInfo key)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Control) ||
                key.Modifiers.HasFlag(ConsoleModifiers.Alt))
            {
                return;
            }

            if (Console.WindowWidth - 1 == _input.Length)
            {
                return;
            }

            _input.Insert(_cursorX, key.KeyChar);
            _out.Write(Ansi.Modification.InsertCharacter);
            _out.Write(key.KeyChar);
            _cursorX++;
            Render();
        }

        private TResult ReadInput()
        {
            Render();
            _isInputAccepted = false;
            while (!_isInputAccepted)
            {
                ConsoleKeyInfo key;
                var oldControlAsInput = Console.TreatControlCAsInput;
                Console.TreatControlCAsInput = true;
                try
                {
                    key = Console.ReadKey(intercept: true);
                }
                finally
                {
                    Console.TreatControlCAsInput = oldControlAsInput;
                }

                switch (key.Key)
                {
                    case ConsoleKey.Backspace:
                        DeleteBackwardWord(key, ConsoleModifiers.Control, DeleteBackward);
                        break;
                    case ConsoleKey.Escape: Clear(); break;
                    case ConsoleKey.Delete: DeleteForwardWord(key, ConsoleModifiers.Control, DeleteForward); break;
                    case ConsoleKey.Enter: Accept(); break;
                    case ConsoleKey.UpArrow: MoveSelectionUp(key); break;
                    case ConsoleKey.DownArrow: MoveSelectionDown(key); break;
                    case ConsoleKey.RightArrow: MoveRight(key); break;
                    case ConsoleKey.LeftArrow: MoveLeft(key); break;
                    case ConsoleKey.P: MoveSelectionUp(key, ConsoleModifiers.Alt); break;
                    case ConsoleKey.N: MoveSelectionDown(key, ConsoleModifiers.Alt); break;
                    case ConsoleKey.C: Break(key, ConsoleModifiers.Control); break;
                    case ConsoleKey.W: DeleteBackwardWord(key, ConsoleModifiers.Control); break;
                    case ConsoleKey.Home: StartOfLine(); break;
                    case ConsoleKey.End: EndOfLine(); break;
                    default: SelfInsert(key); break;
                }
            }

            return GetResult();
        }

        private void MoveLeft(ConsoleKeyInfo key)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                _cursorX = GetBackwardWordOffset();
                SetCursorX(_cursorX);
                return;
            }

            if (_cursorX == 0)
            {
                return;
            }

            _cursorX--;
            _out.Write(Ansi.Movement.CursorLeft);
        }

        private void MoveRight(ConsoleKeyInfo key)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                _cursorX = GetForwardWordOffset();
                SetCursorX(_cursorX);
                return;
            }

            if (_cursorX >= _input.Length - 1)
            {
                return;
            }

            _cursorX++;
            _out.Write(Ansi.Movement.CursorRight);
        }

        private void Break(
            ConsoleKeyInfo key,
            ConsoleModifiers requiredModifiers = default(ConsoleModifiers))
        {
            if (!key.Modifiers.HasFlag(requiredModifiers))
            {
                SelfInsert(key);
                return;
            }

            throw new OperationCanceledException();
        }

        private void StartOfLine()
        {
            _cursorX = 0;
            SetCursorX(0);
        }

        private void EndOfLine()
        {
            _cursorX = _input.Length - 1;
            SetCursorX(_cursorX);
        }

        private void Clear()
        {
            _input.Clear();
            _cursorX = 0;
            Render();
        }

        private void Accept()
        {
            _isInputAccepted = true;
        }

        private void DeleteBackward(ConsoleKeyInfo key)
        {
            if (_input.Length < 1 || _cursorX == 0)
            {
                return;
            }

            _input.Remove(_cursorX - 1, 1);
            _cursorX--;
            _out.Write(Ansi.Movement.CursorLeft);
            _out.Write(Ansi.Modification.DeleteCharacter);
            Render();
        }

        private void DeleteBackwardWord(
            ConsoleKeyInfo key,
            ConsoleModifiers requiredModifiers = default(ConsoleModifiers),
            Action<ConsoleKeyInfo> alternativeAction = null)
        {
            if (!key.Modifiers.HasFlag(requiredModifiers))
            {
                if (alternativeAction != null)
                {
                    alternativeAction(key);
                    return;
                }

                SelfInsert(key);
                return;
            }

            int deleteTo = GetBackwardWordOffset();
            int amountToDelete = _cursorX - deleteTo;
            _input.Remove(deleteTo, amountToDelete);
            _out.Write(Ansi.Movement.MultipleCursorLeft, amountToDelete);
            _out.Write(Ansi.Modification.DeleteCharacters, amountToDelete);
            _cursorX = deleteTo;
            Render();
        }

        private void DeleteForwardWord(
            ConsoleKeyInfo key,
            ConsoleModifiers requiredModifiers = default(ConsoleModifiers),
            Action<ConsoleKeyInfo> alternativeAction = null)
        {
            if (!key.Modifiers.HasFlag(requiredModifiers))
            {
                if (alternativeAction != null)
                {
                    alternativeAction(key);
                    return;
                }

                SelfInsert(key);
                return;
            }

            int deleteTo = GetForwardWordOffset();
            _input.Remove(_cursorX, deleteTo - _cursorX);
            Render();
        }

        private int GetBackwardWordOffset()
        {
            if (_input.Length == 0)
            {
                return 0;
            }

            return Regex.Matches(_input.ToString(), @"\b")
                .Cast<Match>()
                .Where(match => match.Index < _cursorX)
                .OrderByDescending(match => match.Index)
                .First()
                .Index;
        }

        private int GetForwardWordOffset()
        {
            var offset = Regex.Matches(_input.ToString(), @"\b")
                .Cast<Match>()
                .Where(match => match.Index > _cursorX)
                .OrderBy(match => match.Index)
                .FirstOrDefault()
                ?.Index
                ?? -1;

            return offset == -1 ? _input.Length : offset;
        }

        private void DeleteForward(ConsoleKeyInfo key)
        {
            if (_input.Length < 1 || _cursorX == _input.Length)
            {
                return;
            }

            _input.Remove(_cursorX, 1);
            Render();
        }

        private void WriteSubjectScript()
        {
            if (_renderedScript != null)
            {
                _out.Write(_renderedScript);
                _out.WriteLine();
                _out.WriteLine();
                return;
            }

            if (CompletionData == null || string.IsNullOrWhiteSpace(CompletionData.Item1.Extent.Text))
            {
                _renderedScript = string.Empty;
                return;
            }

            Token[] tokens = CompletionData.Item2;
            if (Util.GetStringHeight(CompletionData.Item1.Extent.Text, _lastWindowWidth) > 4)
            {
                Parser.ParseInput(CompletionData.Item3.Line, out tokens, out _);
            }

            _renderedScript = Util.GetRenderedScript(tokens);
            _out.Write(_renderedScript);
            _out.WriteLine();
            _out.WriteLine();
        }

        private void EnsureInputWithinBounds()
        {
            int newMaxLength = Console.WindowWidth - 1;
            if (newMaxLength >= _input.Length)
            {
                return;
            }

            _input.Remove(
                newMaxLength,
                _input.Length - newMaxLength);
        }
    }
}
