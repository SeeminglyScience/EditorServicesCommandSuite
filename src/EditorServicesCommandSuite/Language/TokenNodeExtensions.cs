using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Language
{
    internal static class TokenNodeExtensions
    {
        private enum EnumerationMode
        {
            All,

            At,

            AtOrBefore,

            AtOrAfter,
        }

        public static LinkedListNode<Token> AtOrBefore(
            this LinkedListNode<Token> nodes,
            int offset)
        {
            return new TokenNodeEnumerable(
                nodes?.List.First,
                moveBack: false,
                EnumerationMode.AtOrBefore,
                offset)
                .First();
        }

        public static LinkedListNode<Token> AtOrBefore(
            this LinkedListNode<Token> nodes,
            IScriptPosition position)
        {
            return AtOrBefore(nodes, position.Offset);
        }

        public static LinkedListNode<Token> AtOrBefore(
            this LinkedListNode<Token> nodes,
            IScriptExtent extent,
            bool atEnd = false)
        {
            return AtOrBefore(nodes, atEnd ? extent.EndOffset : extent.StartOffset);
        }

        public static LinkedListNode<Token> AtOrBefore(
            this LinkedListNode<Token> nodes,
            Ast ast,
            bool atEnd = false)
        {
            return AtOrBefore(nodes, atEnd ? ast.Extent.EndOffset : ast.Extent.StartOffset);
        }

        public static LinkedListNode<Token> AtOrAfter(
            this LinkedListNode<Token> nodes,
            int offset)
        {
            return new TokenNodeEnumerable(
                nodes?.List.First,
                moveBack: false,
                EnumerationMode.AtOrAfter,
                offset)
                .First();
        }

        public static LinkedListNode<Token> AtOrAfter(
            this LinkedListNode<Token> nodes,
            IScriptPosition position)
        {
            return AtOrAfter(nodes, position.Offset);
        }

        public static LinkedListNode<Token> AtOrAfter(
            this LinkedListNode<Token> nodes,
            IScriptExtent extent,
            bool atEnd = false)
        {
            return AtOrAfter(nodes, atEnd ? extent.EndOffset : extent.StartOffset);
        }

        public static LinkedListNode<Token> AtOrAfter(
            this LinkedListNode<Token> nodes,
            Ast ast,
            bool atEnd = false)
        {
            return AtOrAfter(nodes, atEnd ? ast.Extent.EndOffset : ast.Extent.StartOffset);
        }

        public static LinkedListNode<Token> At(
            this LinkedListNode<Token> nodes,
            int offset)
        {
            return new TokenNodeEnumerable(
                nodes?.List.First,
                moveBack: false,
                EnumerationMode.At,
                offset)
                .First();
        }

        public static LinkedListNode<Token> At(
            this LinkedListNode<Token> nodes,
            IScriptPosition position)
        {
            return At(nodes, position.Offset);
        }

        public static LinkedListNode<Token> At(
            this LinkedListNode<Token> nodes,
            IScriptExtent extent,
            bool atEnd = false)
        {
            return At(nodes, atEnd ? extent.EndOffset : extent.StartOffset);
        }

        public static LinkedListNode<Token> At(
            this LinkedListNode<Token> nodes,
            Ast ast,
            bool atEnd = false)
        {
            return At(nodes, atEnd ? ast.Extent.EndOffset : ast.Extent.StartOffset);
        }

        public static LinkedListNode<Token> At(
            this LinkedListNode<Token> nodes,
            Token token)
        {
            return nodes.List.Find(token);
        }

        public static LinkedListNode<Token> At(
            this IEnumerable<LinkedListNode<Token>> nodes,
            int offset)
        {
            Validate.IsWithinRange(nameof(offset), offset, 0, int.MaxValue);
            return nodes.FirstOrDefault(node => node.Value.Extent.ContainsOffset(offset));
        }

        public static LinkedListNode<Token> At(
            this IEnumerable<LinkedListNode<Token>> nodes,
            IScriptPosition position)
        {
            Validate.IsNotNull(nameof(position), position);
            return At(nodes, position.Offset);
        }

        public static LinkedListNode<Token> At(
            this IEnumerable<LinkedListNode<Token>> nodes,
            IScriptExtent extent,
            bool atEnd = false)
        {
            Validate.IsNotNull(nameof(extent), extent);
            return At(nodes, atEnd ? extent.EndOffset : extent.StartOffset);
        }

        public static LinkedListNode<Token> At(
            this IEnumerable<LinkedListNode<Token>> nodes,
            Ast ast,
            bool atEnd = false)
        {
            Validate.IsNotNull(nameof(ast), ast);
            return At(nodes, atEnd ? ast.Extent.EndOffset : ast.Extent.StartOffset);
        }

        public static LinkedListNode<Token> NextAt(
            this LinkedListNode<Token> node,
            int offset)
        {
            return At(node.EnumerateNext(), offset);
        }

        public static LinkedListNode<Token> NextAt(
            this LinkedListNode<Token> node,
            IScriptPosition position)
        {
            return At(node.EnumerateNext(), position.Offset);
        }

        public static LinkedListNode<Token> NextAt(
            this LinkedListNode<Token> node,
            IScriptExtent extent,
            bool atEnd = false)
        {
            return At(node.EnumerateNext(), extent, atEnd);
        }

        public static LinkedListNode<Token> NextAt(
            this LinkedListNode<Token> node,
            Ast ast,
            bool atEnd = false)
        {
            return At(node.EnumerateNext(), ast, atEnd);
        }

        public static LinkedListNode<Token> PreviousAt(
            this LinkedListNode<Token> node,
            int offset)
        {
            return At(node.EnumeratePrevious(), offset);
        }

        public static LinkedListNode<Token> PreviousAt(
            this LinkedListNode<Token> node,
            IScriptPosition position)
        {
            return At(node.EnumeratePrevious(), position.Offset);
        }

        public static LinkedListNode<Token> PreviousAt(
            this LinkedListNode<Token> node,
            IScriptExtent extent,
            bool atEnd = false)
        {
            return At(node.EnumeratePrevious(), extent, atEnd);
        }

        public static LinkedListNode<Token> PreviousAt(
            this LinkedListNode<Token> node,
            Ast ast,
            bool atEnd = false)
        {
            return At(node.EnumeratePrevious(), ast, atEnd);
        }

        public static IEnumerable<LinkedListNode<Token>> StartAt(
            this LinkedListNode<Token> node,
            int offset)
        {
            return At(node, offset).EnumerateNext();
        }

        public static IEnumerable<LinkedListNode<Token>> StartAt(
            this LinkedListNode<Token> node,
            IScriptPosition position)
        {
            return At(node, position).EnumerateNext();
        }

        public static IEnumerable<LinkedListNode<Token>> StartAt(
            this LinkedListNode<Token> node,
            IScriptExtent extent)
        {
            return At(node, extent).EnumerateNext();
        }

        public static IEnumerable<LinkedListNode<Token>> StartAt(
            this LinkedListNode<Token> node,
            Ast ast)
        {
            return At(node, ast).EnumerateNext();
        }

        public static IEnumerable<LinkedListNode<Token>> StartAt(
            this LinkedListNode<Token> node,
            Token token)
        {
            return At(node, token.Extent).EnumerateNext();
        }

        public static IEnumerable<LinkedListNode<Token>> StartAtEndOf(
            this LinkedListNode<Token> node,
            IScriptExtent extent)
        {
            return At(node, extent, atEnd: true).EnumerateNext();
        }

        public static IEnumerable<LinkedListNode<Token>> StartAtEndOf(
            this LinkedListNode<Token> node,
            Ast ast)
        {
            return At(node, ast, atEnd: true).EnumerateNext();
        }

        public static IEnumerable<LinkedListNode<Token>> EnumerateAll(this LinkedListNode<Token> node)
        {
            return new TokenNodeEnumerable(node?.List.First, moveBack: false);
        }

        public static IEnumerable<LinkedListNode<Token>> EnumerateNext(this LinkedListNode<Token> node)
        {
            return new TokenNodeEnumerable(node?.Next, moveBack: false);
        }

        public static IEnumerable<LinkedListNode<Token>> EnumeratePrevious(this LinkedListNode<Token> node)
        {
            return new TokenNodeEnumerable(node?.Previous, moveBack: true);
        }

        public static LinkedListNode<Token> FindNext(
            this LinkedListNode<Token> node,
            Func<LinkedListNode<Token>, bool> predicate)
        {
            Validate.IsNotNull(nameof(predicate), predicate);
            return node.EnumerateNext().FirstOrDefault(predicate);
        }

        public static LinkedListNode<Token> FindPrevious(
            this LinkedListNode<Token> node,
            Func<LinkedListNode<Token>, bool> predicate)
        {
            Validate.IsNotNull(nameof(predicate), predicate);
            return node.EnumeratePrevious().FirstOrDefault(predicate);
        }

        private class TokenNodeEnumerable : IEnumerable<LinkedListNode<Token>>
        {
            private readonly LinkedListNode<Token> _initialNode;

            private readonly bool _moveBack;

            private readonly EnumerationMode _mode;

            private readonly int _targetOffset;

            internal TokenNodeEnumerable(LinkedListNode<Token> initialNode, bool moveBack)
                : this(initialNode, moveBack, EnumerationMode.All, 0)
            {
            }

            internal TokenNodeEnumerable(
                LinkedListNode<Token> initialNode,
                bool moveBack,
                EnumerationMode mode,
                int targetOffset)
            {
                _initialNode = initialNode;
                _moveBack = moveBack;
                _mode = mode;
                _targetOffset = targetOffset;
            }

            public IEnumerator<LinkedListNode<Token>> GetEnumerator()
            {
                switch (_mode)
                {
                    case EnumerationMode.At:
                        return new TokenNodeAtEnumerator(_targetOffset, _initialNode, _moveBack);
                    case EnumerationMode.AtOrBefore:
                        return new TokenNodeAtOrBeforeEnumerator(_targetOffset, _initialNode, _moveBack);
                    case EnumerationMode.AtOrAfter:
                        return new TokenNodeAtOrAfterEnumerator(_targetOffset, _initialNode, _moveBack);
                    default:
                        return new TokenNodeEnumerator(_initialNode, _moveBack);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private class TokenNodeEnumerator : IEnumerator<LinkedListNode<Token>>
        {
            protected LinkedListNode<Token> _current;

            private readonly LinkedListNode<Token> _initial;

            private readonly bool _moveBack;

            private bool _first = true;

            internal TokenNodeEnumerator(LinkedListNode<Token> initialNode, bool moveBack)
            {
                _initial = _current = initialNode;
                _moveBack = moveBack;
            }

            public LinkedListNode<Token> Current => _current;

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                _current = null;
            }

            public virtual bool MoveNext()
            {
                if (_first)
                {
                    _first = false;
                    _current = _initial;
                    return _current != null;
                }

                return _current != null
                    && (_current = _moveBack ? _current.Previous : _current.Next) != null;
            }

            public void Reset()
            {
                _current = null;
                _first = true;
            }
        }

        private class TokenNodeAtEnumerator : TokenNodeEnumerator
        {
            private readonly int _targetOffset;

            internal TokenNodeAtEnumerator(
                int targetOffset,
                LinkedListNode<Token> initialNode,
                bool moveBack)
                : base(initialNode, moveBack)
            {
                _targetOffset = targetOffset;
            }

            public override bool MoveNext()
            {
                while (base.MoveNext() && !Current.Value.Extent.ContainsOffset(_targetOffset))
                {
                    if (_current.Value.Extent.EndOffset > _targetOffset)
                    {
                        _current = null;
                        return false;
                    }
                }

                return Current != null;
            }
        }

        private class TokenNodeAtOrBeforeEnumerator : TokenNodeEnumerator
        {
            private readonly int _targetOffset;

            private LinkedListNode<Token> _last;

            private bool _outOfRange;

            internal TokenNodeAtOrBeforeEnumerator(
                int targetOffset,
                LinkedListNode<Token> initialNode,
                bool moveBack)
                : base(initialNode, moveBack)
            {
                _targetOffset = targetOffset;
            }

            public override bool MoveNext()
            {
                if (_outOfRange)
                {
                    _current = null;
                    return false;
                }

                while (base.MoveNext() && !_current.Value.Extent.ContainsOffset(_targetOffset))
                {
                    if (_current.Value.Extent.EndOffset > _targetOffset)
                    {
                        _outOfRange = true;
                        _current = _last;
                        break;
                    }

                    _last = _current;
                }

                return _current != null;
            }
        }

        private class TokenNodeAtOrAfterEnumerator : TokenNodeEnumerator
        {
            private readonly int _targetOffset;

            private bool _outOfRange;

            internal TokenNodeAtOrAfterEnumerator(
                int targetOffset,
                LinkedListNode<Token> initialNode,
                bool moveBack)
                : base(initialNode, moveBack)
            {
                _targetOffset = targetOffset;
            }

            public override bool MoveNext()
            {
                if (_outOfRange)
                {
                    _current = null;
                    return false;
                }

                while (base.MoveNext() && !_current.Value.Extent.ContainsOffset(_targetOffset))
                {
                    if (_current.Value.Extent.EndOffset > _targetOffset)
                    {
                        _outOfRange = true;
                        break;
                    }
                }

                return _current != null;
            }
        }
    }
}
