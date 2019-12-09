using System;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Runtime.CompilerServices;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Language
{
    internal readonly struct TokenNode : IEquatable<TokenNode>
    {
        internal readonly ReadOnlyMemory<Token> Memory;

        internal readonly int Index;

        private TokenNode(ReadOnlyMemory<Token> tokens, int index)
        {
            if (tokens.IsEmpty)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            if (!IsValidIndex(tokens, index))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            Memory = tokens;
            Index = index;
        }

        public bool IsDefault => Memory.IsEmpty && Index == 0;

        public TokenCollection List => new TokenCollection(Memory);

        public TokenNode Previous => GetSiblingNode(isPrevious: true, throwOnFailure: true);

        public TokenNode? PreviousOrNull
        {
            get
            {
                TokenNode? node = GetSiblingNode(isPrevious: true, throwOnFailure: false);
                return node == default(TokenNode) ? null : node;
            }
        }

        public TokenNode Next => GetSiblingNode(isPrevious: false, throwOnFailure: true);

        public TokenNode? NextOrNull
        {
            get
            {
                TokenNode? node = GetSiblingNode(isPrevious: false, throwOnFailure: false);
                return node == default(TokenNode) ? null : node;
            }
        }

        public Token Value
        {
            get
            {
                if (IsDefault)
                {
                    throw Error.AttemptedAccessDefaultTokenNode();
                }

                return Memory.Span[Index];
            }
        }

        public TokenKind Kind => Value.Kind;

        public static implicit operator Token(TokenNode source) => source.Value;

        public static bool operator ==(TokenNode left, TokenNode right)
            => left.Equals(right);

        public static bool operator !=(TokenNode left, TokenNode right)
            => !left.Equals(right);

        public static TokenNode Create(ReadOnlyMemory<Token> tokens, int index)
        {
            if (tokens.IsEmpty)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            if (!IsValidIndex(tokens, index))
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return new TokenNode(tokens, index);
        }

        public bool Equals(TokenNode other) => Value == other.Value;

        public override bool Equals(object obj)
            => obj is TokenNode node
                ? Equals(node)
                : obj is Token token && token == Value;

        public override int GetHashCode() => Value?.GetHashCode() ?? 0;

        public bool TryGetPrevious(out TokenNode node)
        {
            node = this;
            return TryMovePrevious(ref node);
        }

        public bool TryGetNext(out TokenNode node)
        {
            node = this;
            return TryMoveNext(ref node);
        }

        internal static bool TryMovePrevious(ref TokenNode token)
        {
            if (token.Index == 0)
            {
                return false;
            }

            Unsafe.AsRef(in token.Index)--;
            return true;
        }

        internal static bool TryMoveNext(ref TokenNode token)
        {
            int nextIndex = token.Index + 1;
            if (!IsValidIndex(token.Memory, nextIndex))
            {
                return false;
            }

            Unsafe.AsRef(in token.Index)++;
            return true;
        }

        internal TokenNode CloneWithNewIndex(int index) => GetNode(index);

        internal TokenFinder FindPrevious() => new TokenFinder(this, TokenFinderDirection.Previous);

        internal TokenFinder FindPreviousOrSelf() => new TokenFinder(this, TokenFinderDirection.Previous, includeSelf: true);

        internal TokenFinder FindNext() => new TokenFinder(this, TokenFinderDirection.Next);

        internal TokenFinder FindNextOrSelf() => new TokenFinder(this, TokenFinderDirection.Next, includeSelf: true);

        private static bool IsValidIndex(ReadOnlyMemory<Token> memory, int index)
        {
            return index >= 0 && index < memory.Length;
        }

        private TokenNode GetSiblingNode(bool isPrevious = false, bool throwOnFailure = false)
        {
            return GetNode(
                isPrevious ? Index - 1 : Index + 1,
                throwOnFailure);
        }

        private TokenNode GetNode(int index, bool throwOnFailure = false)
        {
            if (Memory.IsEmpty || !IsValidIndex(Memory, index))
            {
                if (throwOnFailure)
                {
                    throw Error.TokenNotFound();
                }

                return default;
            }

            return new TokenNode(Memory, index);
        }
    }
}
