using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration
{
    internal class DocumentEditWriter : StreamWriter
    {
        protected int _implicitOverrideCount;

        protected string _coreTabString;

        protected char[] _coreTab;

        private readonly Stack<int> _indentStack = new Stack<int>();

        private readonly int _byteOffsetModifier;

        private readonly char[] _drainBuffer = new char[256];

        private readonly List<DocumentEdit> _edits = new List<DocumentEdit>();

        private readonly List<DocumentEdit> _editsToBeReduced = new List<DocumentEdit>();

        private int? _pendingIndent;

        private bool _isWritePending;

        private long _lastPositionSet;

        private MemoryStream _originalDocument;

        private byte[] _originalBuffer;

        public DocumentEditWriter()
            : this(string.Empty)
        {
        }

        public DocumentEditWriter(string initialValue)
            : base(new MemoryStream(), DefaultEncoding)
        {
            _byteOffsetModifier = Encoding.IsSingleByte ? 1 : 2;
            TabString = Settings.TabString;
            NewLine = Settings.NewLine;
            _originalDocument = new MemoryStream();
            if (string.IsNullOrEmpty(initialValue))
            {
                _originalBuffer = new byte[0];
                return;
            }

            var writer = new StreamWriter(_originalDocument, Encoding);
            writer.Write(initialValue);
            writer.Flush();
            _originalBuffer = (byte[])_originalDocument.GetBuffer().Clone();

            _lastPositionSet = _originalDocument.Position / _byteOffsetModifier;
        }

        public virtual int Indent { get; set; }

        public virtual string TabString
        {
            get
            {
                return _coreTabString;
            }

            set
            {
                if (value == null)
                {
                    return;
                }

                _coreTabString = value;
                _coreTab = value.ToArray();
            }
        }

        internal static Encoding DefaultEncoding { get; set; } = Encoding.ASCII;

        internal virtual IEnumerable<DocumentEdit> Edits
        {
            get
            {
                DrainPendingEdits();
                CreateDocumentEdits();
                return _edits.AsReadOnly();
            }
        }

        public virtual void StartWriting(int startOffset, int endOffset)
        {
            SetPosition(startOffset);
            if (startOffset >= endOffset)
            {
                return;
            }

            _implicitOverrideCount = endOffset - startOffset;
        }

        public virtual void FinishWriting()
        {
            CreateDocumentEdits(_implicitOverrideCount);
            _implicitOverrideCount = 0;
        }

        public virtual void FinishWriting(int overrideCount)
        {
            CreateDocumentEdits(overrideCount);
        }

        public virtual void SetPosition(int offset)
        {
            if (_isWritePending)
            {
                CreatePendingEdit(_implicitOverrideCount);
                _implicitOverrideCount = 0;
            }

            _lastPositionSet = offset < 0 ? 0 : offset;
        }

        public virtual void CreateDocumentEdits()
        {
            CreateDocumentEdits(0);
        }

        public virtual void CreateDocumentEdits(int overwriteCount)
        {
            CreateDocumentEditsImpl(overwriteCount);
        }

        public virtual void WriteLineNoIndent() => BaseWrite(() => base.WriteLine());

        internal virtual void PushIndent(int amount = 1)
        {
            _indentStack.Push(Indent);
            Indent = Indent + amount;
        }

        internal virtual void PopIndent()
        {
            Indent = _indentStack.Pop();
        }

        internal void WriteIndentIfPending()
        {
            if (_pendingIndent == null || _pendingIndent == 0)
            {
                return;
            }

            int indent = _pendingIndent.Value;
            _pendingIndent = null;
            for (var i = 1; i <= indent; i++)
            {
                Write(_coreTab);
            }
        }

        internal void WriteChars(char first, char second)
        {
            WriteIndentIfPending();
            base.Write(first);
            base.Write(second);
            _isWritePending = true;
        }

        internal void WriteChars(char first, char second, char third)
        {
            WriteIndentIfPending();
            base.Write(first);
            base.Write(second);
            base.Write(third);
            _isWritePending = true;
        }

        internal void WriteChars(char first, char second, char third, char fourth)
        {
            WriteIndentIfPending();
            base.Write(first);
            base.Write(second);
            base.Write(third);
            base.Write(fourth);
            _isWritePending = true;
        }

        internal void WriteChars(params char[] buffer)
        {
            WriteIndentIfPending();
            base.Write(buffer);
            _isWritePending = true;
        }

        internal virtual void FrameOpen()
        {
            PushIndent();
            WriteLine();
        }

        internal virtual void FrameClose()
        {
            PopIndent();
            WriteLine();
        }

        internal virtual void WriteEachWithSeparator<TInput>(
            IList<TInput> source,
            Action<TInput> writer,
            char[] separator)
        {
            WriteEachWithSeparator<TInput>(
                source,
                writer,
                () => Write(separator));
        }

        internal virtual void WriteEachWithSeparator<TInput>(
            IList<TInput> source,
            Action<TInput> writer,
            string separator)
        {
            WriteEachWithSeparator<TInput>(
                source,
                writer,
                () => Write(separator));
        }

        internal virtual void WriteEachWithSeparator<TInput>(
            IList<TInput> source,
            Action<TInput> writer,
            Action separationWriter)
        {
            if (!source.Any())
            {
                return;
            }

            if (source.Count == 1)
            {
                writer(source[0]);
                return;
            }

            for (var i = 0; i < source.Count; i++)
            {
                writer(source[i]);
                if (i < source.Count - 1)
                {
                    separationWriter();
                }
            }
        }

        internal virtual void WriteLines(int amount)
        {
            for (var i = 0; i < amount; i++)
            {
                WriteLine();
            }
        }

        internal virtual void WriteLines(IEnumerable<string> lines)
        {
            WriteEachWithSeparator(
                lines.ToArray(),
                line => Write(line),
                () => WriteLine());
        }

        internal void WriteWrappedLines(string text, int length, Action separator)
        {
            WriteWrappedLines(
                text.ToCharArray(),
                length,
                separator);
        }

        internal void WriteWrappedLines(char[] chars, int length, Action separator)
        {
            var lineStart = 0;
            var column = 0;
            int? lastWhiteSpace = null;
            bool justForcedNewLine = false;
            for (var i = 0; i < chars.Length; i++, column++)
            {
                if (chars[i] == '\n')
                {
                    if (justForcedNewLine)
                    {
                        lineStart = i + 1;
                        column = -1;
                        justForcedNewLine = false;
                        continue;
                    }

                    int extraOffset = i != 0 && chars[i - 1] == '\r' ? 1 : 0;
                    Write(chars, lineStart, column - extraOffset);
                    separator();
                    lineStart = i + 1;
                    column = -1;
                    lastWhiteSpace = null;
                    continue;
                }

                if (chars[i] == '\r')
                {
                    continue;
                }

                if (char.IsWhiteSpace(chars[i]))
                {
                    lastWhiteSpace = column;
                }

                if (column >= length && lastWhiteSpace != null)
                {
                    Write(chars, lineStart, lastWhiteSpace.Value);
                    separator();
                    column = lastWhiteSpace == column ? 0 : column - lastWhiteSpace.Value - 1;
                    lineStart = lineStart + lastWhiteSpace.Value + 1;
                    lastWhiteSpace = null;
                    justForcedNewLine = true;
                    continue;
                }

                justForcedNewLine = false;
            }

            Write(chars, lineStart, chars.Length - lineStart);
        }

        protected void CreateDocumentEditsImpl(int overwriteCount)
        {
            CreatePendingEdit(overwriteCount);
        }

        private string DrainChars(int overwriteCount, StreamReader reader)
        {
            var sb = new StringBuilder(overwriteCount);
            while (overwriteCount != sb.Length)
            {
                sb.Append(
                    _drainBuffer,
                    0,
                    reader.Read(
                        _drainBuffer,
                        0,
                        Math.Min(
                            overwriteCount - sb.Length,
                            _drainBuffer.Length)));

                if (sb.Length == 0)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            DocumentEditWriterStrings.UnexpectedWriterFailure,
                            DocumentEditWriterStrings.NoProgressInDrainChars));
                }
            }

            return sb.ToString();
        }

        private void CreatePendingEdit(int overwriteCount)
        {
            if (!_isWritePending)
            {
                return;
            }

            _editsToBeReduced.Add(CreateEdit(overwriteCount));
        }

        private DocumentEdit CreateEdit(int overwriteCount)
        {
            Flush();
            var reader = new StreamReader(_originalDocument, Encoding);
            string restOfStream = string.Empty;
            long currentPosition = 0;
            var edit = new DocumentEdit()
            {
                StartOffset = _lastPositionSet,
                EndOffset = _lastPositionSet + overwriteCount,
            };

            _originalDocument.Seek(
                _lastPositionSet * _byteOffsetModifier,
                SeekOrigin.Begin);
            if (overwriteCount > 0)
            {
                edit.OriginalValue = DrainChars(overwriteCount, reader);
            }

            // Save the rest so we can append after write.
            restOfStream = reader.ReadToEnd();
            reader.DiscardBufferedData();

            // Write pending changes
            _originalDocument.Seek(_lastPositionSet, SeekOrigin.Begin);
            BaseStream.Position = 0;
            BaseStream.CopyTo(_originalDocument);
            _originalDocument.Flush();

            // Save the offset and go back so we can read the new value.
            var baseReader = new StreamReader(BaseStream, Encoding);
            BaseStream.Position = 0;
            edit.NewValue = baseReader.ReadToEnd();
            baseReader.DiscardBufferedData();

            currentPosition = _originalDocument.Position / _byteOffsetModifier;

            // Save the edit and write the append the rest of the stream.
            _isWritePending = false;
            reader.DiscardBufferedData();

            BaseStream.Position = 0;
            BaseStream.SetLength(0);
            _originalDocument.Position = 0;
            _originalDocument.SetLength(0);
            _originalDocument.Write(_originalBuffer, 0, _originalBuffer.Length);
            _originalDocument.Flush();

            return edit;
        }

        private void DrainPendingEdits()
        {
            if (_isWritePending)
            {
                CreatePendingEdit(_implicitOverrideCount);
                _isWritePending = true;
            }

            if (!_editsToBeReduced.Any())
            {
                return;
            }

            _edits.AddRange(ReduceEdits(_editsToBeReduced));
            _editsToBeReduced.Clear();
        }

        private IEnumerable<DocumentEdit> ReduceEdits(IEnumerable<DocumentEdit> edits)
        {
            foreach (var editGroup in edits.GroupBy(edit => edit.StartOffset))
            {
                var highestOverride = editGroup
                    .OrderByDescending(edit => edit.EndOffset)
                    .First();

                yield return new DocumentEdit()
                {
                    StartOffset = editGroup.Key,
                    EndOffset = highestOverride.EndOffset,
                    OriginalValue = highestOverride.OriginalValue,
                    NewValue = string.Join(
                        string.Empty,
                        editGroup
                            .OrderBy(edit => edit.Id)
                            .Select(edit => edit.NewValue)),
                };
            }
        }

        private void BaseWrite(Action writer)
        {
            WriteIndentIfPending();
            writer();
            _isWritePending = true;
        }

        private async Task BaseWriteAsync(Func<Task> writer)
        {
            await WriteIndentIfPendingAsync();
            await writer();
            _isWritePending = true;
        }

        private void BaseWriteLine(Action writer = null)
        {
            writer?.Invoke();

            // If we use base.WriteLine here (or in the delegate) it'll call out to our
            // Write method. This causes pending indents to be written unnecessarily.
            base.Write(CoreNewLine);
            _isWritePending = true;
            _pendingIndent = Indent;
        }

        private async Task BaseWriteLineAsync(Func<Task> writer = null)
        {
            if (writer != null)
            {
                await writer();
            }

            // If we use base.WriteLineAsync here (or in the delegate) it'll call out to our
            // Write method. This causes pending indents to be written unnecessarily.
            await WriteAsync(CoreNewLine);
            _isWritePending = true;
            _pendingIndent = Indent;
        }

        private async Task WriteIndentIfPendingAsync()
        {
            if (_pendingIndent == null || _pendingIndent == 0)
            {
                return;
            }

            int indent = _pendingIndent.Value;
            _pendingIndent = null;
            for (var i = 1; i <= indent; i++)
            {
                await WriteAsync(_coreTab);
            }
        }

#pragma warning disable SA1202
        public override void WriteLine() => BaseWriteLine();

        public override void WriteLine(bool value) => BaseWriteLine(() => base.Write(value));

        public override void WriteLine(char value) => BaseWriteLine(() => base.Write(value));

        public override void WriteLine(params char[] buffer) => BaseWriteLine(() => base.Write(buffer));

        public override void WriteLine(char[] buffer, int index, int count) => BaseWriteLine(() => base.Write(buffer, index, count));

        public override void WriteLine(decimal value) => BaseWriteLine(() => base.Write(value));

        public override void WriteLine(double value) => BaseWriteLine(() => base.Write(value));

        public override void WriteLine(float value) => BaseWriteLine(() => base.Write(value));

        public override void WriteLine(int value) => BaseWriteLine(() => base.Write(value));

        public override void WriteLine(long value) => BaseWriteLine(() => base.Write(value));

        public override void WriteLine(object value) => BaseWriteLine(() => base.Write(value));

        public override void WriteLine(string format, object arg0) => BaseWriteLine(() => base.Write(format, arg0));

        public override void WriteLine(string format, object arg0, object arg1) => BaseWriteLine(() => base.Write(format, arg0, arg1));

        public override void WriteLine(string format, object arg0, object arg1, object arg2) => BaseWriteLine(() => base.Write(format, arg0, arg1, arg2));

        public override void WriteLine(string format, params object[] arg) => BaseWriteLine(() => base.Write(arg));

        public override void WriteLine(string value) => BaseWriteLine(() => base.Write(value));

        public override void WriteLine(uint value) => BaseWriteLine(() => base.Write(value));

        public override void WriteLine(ulong value) => BaseWriteLine(() => base.Write(value));

        public override void Write(bool value) => BaseWrite(() => base.Write(value));

        public override void Write(char value) => BaseWrite(() => base.Write(value));

        public override void Write(params char[] buffer) => BaseWrite(() => base.Write(buffer));

        public override void Write(char[] buffer, int index, int count) => BaseWrite(() => base.Write(buffer, index, count));

        public override void Write(decimal value) => BaseWrite(() => base.Write(value));

        public override void Write(double value) => BaseWrite(() => base.Write(value));

        public override void Write(float value) => BaseWrite(() => base.Write(value));

        public override void Write(int value) => BaseWrite(() => base.Write(value));

        public override void Write(long value) => BaseWrite(() => base.Write(value));

        public override void Write(object value) => BaseWrite(() => base.Write(value));

        public override void Write(string format, object arg0) => BaseWrite(() => base.Write(format, arg0));

        public override void Write(string format, object arg0, object arg1) => BaseWrite(() => base.Write(format, arg0, arg1));

        public override void Write(string format, object arg0, object arg1, object arg2) => BaseWrite(() => base.Write(format, arg0, arg1, arg2));

        public override void Write(string format, params object[] arg) => BaseWrite(() => base.Write(format, arg));

        public override void Write(string value) => BaseWrite(() => base.Write(value));

        public override void Write(uint value) => BaseWrite(() => base.Write(value));

        public override void Write(ulong value) => BaseWrite(() => base.Write(value));

        public override Task WriteAsync(char value) => BaseWriteAsync(() => base.WriteAsync(value));

        public override Task WriteAsync(char[] buffer, int index, int count) => BaseWriteAsync(() => base.WriteAsync(buffer, index, count));

        public override Task WriteAsync(string value) => BaseWriteAsync(() => base.WriteAsync(value));

        public override Task WriteLineAsync() => BaseWriteLineAsync(() => base.WriteLineAsync());

        public override Task WriteLineAsync(char value) => BaseWriteLineAsync(() => base.WriteLineAsync(value));

        public override Task WriteLineAsync(char[] buffer, int index, int count) => BaseWriteLineAsync(() => base.WriteLineAsync(buffer, index, count));

        public override Task WriteLineAsync(string value) => BaseWriteLineAsync(() => base.WriteLineAsync(value));
#pragma warning restore SA1202
    }
}
