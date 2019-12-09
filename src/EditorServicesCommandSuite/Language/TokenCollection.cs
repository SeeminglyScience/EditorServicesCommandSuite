using System;
using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal readonly struct TokenCollection
    {
        private readonly ReadOnlyMemory<Token> _tokens;

        internal TokenCollection(ReadOnlyMemory<Token> tokens)
        {
            _tokens = tokens;
        }

        public bool IsEmpty => _tokens.IsEmpty;

        public int Length => _tokens.Length;

        public TokenNode First => IsEmpty ? default : TokenNode.Create(_tokens, index: 0);

        public TokenNode Last => IsEmpty ? default : TokenNode.Create(_tokens, index: _tokens.Length - 1);

        public TokenCollection Slice(int start) => new TokenCollection(_tokens.Slice(start));

        public TokenCollection Slice(int start, int length) => new TokenCollection(_tokens.Slice(start, length));

        public TokenCollection Slice(IScriptExtent extent) => Slice(new SimpleRange(extent));

        public TokenCollection Slice(SimpleRange range)
        {
            ReadOnlySpan<Token> span = _tokens.Span;
            int index = 0;
            int? length = null;
            bool foundStart = false;
            for (int i = 0; i < span.Length; i++)
            {
                Token current = span[i];
                if (!foundStart)
                {
                    if (range.Start > current.Extent.StartOffset)
                    {
                        continue;
                    }

                    index = i;
                    foundStart = true;
                }

                if (range.End < current.Extent.EndOffset)
                {
                    length = i - index - 1;
                    break;
                }
            }

            if (!foundStart)
            {
                return default;
            }

            return length == null ? Slice(index) : Slice(index, length.Value);
        }
    }
}
