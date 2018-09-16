using System;
using System.Buffers;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using static System.Linq.Expressions.Expression;

namespace EditorServicesCommandSuite.CodeGeneration
{
    internal class SpanEnabledStreamWriter : StreamWriter
    {
        protected static readonly WriteSpanProxy WriteSpan;

        protected static readonly bool IsLegacySpanSupport;

        static SpanEnabledStreamWriter()
        {
            ParameterExpression writer = Parameter(typeof(SpanEnabledStreamWriter), nameof(writer));
            ParameterExpression buffer = Parameter(typeof(ReadOnlySpan<char>), nameof(buffer));
            ParameterExpression appendNewLine = Parameter(typeof(bool), nameof(appendNewLine));

            MethodInfo realWriteSpan = typeof(StreamWriter)
                .GetMethod(
                    "WriteSpan",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(ReadOnlySpan<char>), typeof(bool) },
                    new[] { new ParameterModifier(2) });

            if (realWriteSpan == null)
            {
                WriteSpan = WriteSpanLegacy;
                IsLegacySpanSupport = true;
                return;
            }

            WriteSpan = Lambda<WriteSpanProxy>(
                Call(
                    writer,
                    realWriteSpan,
                    buffer,
                    appendNewLine),
                writer,
                buffer,
                appendNewLine)
                .Compile();
        }

        public SpanEnabledStreamWriter(Stream stream)
            : base(stream)
        {
        }

        public SpanEnabledStreamWriter(string path)
            : base(path)
        {
        }

        public SpanEnabledStreamWriter(Stream stream, Encoding encoding)
            : base(stream, encoding)
        {
        }

        public SpanEnabledStreamWriter(string path, bool append)
            : base(path, append)
        {
        }

        public SpanEnabledStreamWriter(Stream stream, Encoding encoding, int bufferSize)
            : base(stream, encoding, bufferSize)
        {
        }

        public SpanEnabledStreamWriter(string path, bool append, Encoding encoding)
            : base(path, append, encoding)
        {
        }

        public SpanEnabledStreamWriter(Stream stream, Encoding encoding, int bufferSize, bool leaveOpen)
            : base(stream, encoding, bufferSize, leaveOpen)
        {
        }

        public SpanEnabledStreamWriter(string path, bool append, Encoding encoding, int bufferSize)
            : base(path, append, encoding, bufferSize)
        {
        }

        protected delegate void WriteSpanProxy(
            SpanEnabledStreamWriter writer,
            ReadOnlySpan<char> buffer,
            bool appendNewLine);

        public virtual void Write(ReadOnlySpan<char> buffer)
        {
            WriteSpan(this, buffer, appendNewLine: false);
        }

        public virtual void WriteLine(ReadOnlySpan<char> value)
        {
            WriteSpan(this, value, appendNewLine: true);
        }

        private static void WriteSpanLegacy(SpanEnabledStreamWriter writer, ReadOnlySpan<char> buffer, bool appendNewLine)
        {
            char[] charBuffer = ArrayPool<char>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(charBuffer.AsSpan());
                if (appendNewLine)
                {
                    writer.WriteLine(charBuffer, 0, buffer.Length);
                    return;
                }

                writer.Write(charBuffer, 0, buffer.Length);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(charBuffer);
            }
        }
    }
}
