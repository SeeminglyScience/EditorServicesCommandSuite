using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using System.Text;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Reflection;
using EditorServicesCommandSuite.Utility;

using static EditorServicesCommandSuite.Internal.Symbols;

namespace EditorServicesCommandSuite.CodeGeneration
{
    internal class PowerShellScriptWriter : DocumentEditWriter
    {
        private static readonly char[] s_invalidVariableNameChars =
        {
            '!', '@', '#', '$', '%', '^', '*', '(', ')', '+', '=', '|',
            '\\', '/', '\'', '"', '.', ',', '{', '}', '~', '`', ' ', '\t',
            '\r', '\n',
        };

        private static readonly ScriptBlockAst s_emptyAst = new ScriptBlockAst(
            Empty.Extent,
            new ParamBlockAst(
                Empty.Extent,
                Enumerable.Empty<AttributeAst>(),
                Enumerable.Empty<ParameterAst>()),
            new StatementBlockAst(
                Empty.Extent,
                Enumerable.Empty<StatementAst>(),
                Enumerable.Empty<TrapStatementAst>()),
            isFilter: false);

        private readonly HashSet<string> _pendingUsingWrites = new HashSet<string>();

        private ScriptBlockAst _ast;

        private DocumentContextBase _context;

        private bool? _shouldDropNamespaces;

        public PowerShellScriptWriter()
            : base()
        {
            _ast = s_emptyAst;
        }

        public PowerShellScriptWriter(string initialValue)
            : base(initialValue)
        {
            if (string.IsNullOrEmpty(initialValue))
            {
                _ast = s_emptyAst;
                return;
            }

            _ast = Parser.ParseInput(initialValue, out _, out _);
        }

        public PowerShellScriptWriter(Ast ast)
            : base(GetDocumentText(ast.FindRootAst()))
        {
            _ast = ast.FindRootAst();
        }

        public PowerShellScriptWriter(DocumentContextBase context)
            : base(GetDocumentText(context.RootAst))
        {
            _context = context;
            _ast = context.RootAst;
        }

        private bool ShouldDropNamespaces
        {
            get
            {
                return _shouldDropNamespaces
                    ?? (_shouldDropNamespaces = Settings.EnableAutomaticNamespaceRemoval).Value;
            }

            set
            {
                _shouldDropNamespaces = value;
            }
        }

        public override void CreateDocumentEdits(int overwriteCount)
        {
            Flush();
            CreateDocumentEditsImpl(overwriteCount);

            if (!_pendingUsingWrites.Any())
            {
                return;
            }

            if (AddUsingStatements(_pendingUsingWrites, out int replaceLength))
            {
                CreateDocumentEditsImpl(replaceLength);
            }

            _pendingUsingWrites.Clear();
        }

        public bool AddUsingStatements(HashSet<string> namespaces, out int replaceLength)
        {
            replaceLength = 0;
            var existing = _ast.FindAllAsts<UsingStatementAst>();
            namespaces
                .ExceptWith(
                    existing
                        .Where(s => s.UsingStatementKind == UsingStatementKind.Namespace)
                        .Select(s => s.Name.Value));

            var usings = namespaces.Select(
                ns => new UsingDescription()
                {
                    Text = ns,
                    Kind = UsingStatementKind.Namespace,
                });

            if (!namespaces.Any())
            {
                return false;
            }

            if (existing.Any())
            {
                var combinedExtents = existing.JoinExtents();
                replaceLength = combinedExtents.Text.Length;
                SetPosition(combinedExtents);

                usings = usings.Concat(existing.ToDescriptions());
            }
            else
            {
                SetPosition(0);
            }

            Write(UsingUtilities.GetUsingStatementString(usings));

            if (!existing.Any())
            {
                WriteLines(2);
            }

            return true;
        }

        public override void Write(params char[] buffer) => base.Write(buffer);

        public override void StartWriting(int startOffset, int endOffset)
        {
            SetPosition(startOffset);
            if (startOffset >= endOffset)
            {
                return;
            }

            _implicitOverrideCount = endOffset - startOffset;
        }

        public void StartWriting(IScriptPosition startPosition, int endOffset)
        {
            SetPosition(startPosition);
            if (startPosition.Offset >= endOffset)
            {
                return;
            }

            _implicitOverrideCount = endOffset - startPosition.Offset;
        }

        public void StartWriting(IScriptExtent extent)
        {
            StartWriting(extent.StartScriptPosition, extent.EndOffset);
        }

        public void StartWriting(Token token)
        {
            StartWriting(token.Extent.StartScriptPosition, token.Extent.EndOffset);
        }

        public void StartWriting(LinkedListNode<Token> token)
        {
            StartWriting(token.Value.Extent.StartScriptPosition, token.Value.Extent.EndOffset);
        }

        public void StartWriting(Ast ast)
        {
            StartWriting(ast.Extent.StartScriptPosition, ast.Extent.EndOffset);
        }

        public void SetPosition(int offset, bool atEnd = false)
        {
            SetPosition(
                _ast.Extent.StartScriptPosition.CloneWithNewOffset(offset),
                atEnd);
        }

        internal void SetPosition(LinkedListNode<Token> token, bool atEnd = false)
        {
            SetPosition(token.Value.Extent, atEnd);
        }

        internal void SetPosition(Token token, bool atEnd = false)
        {
            SetPosition(token.Extent, atEnd);
        }

        internal void SetPosition(Ast ast, bool atEnd = false)
        {
            SetPosition(ast.Extent, atEnd);
        }

        internal void SetPosition(IScriptExtent extent, bool atEnd = false)
        {
            SetPosition(
                atEnd ? extent.EndScriptPosition : extent.StartScriptPosition,
                atEnd);
        }

        internal void SetPosition(IScriptPosition position, bool atEnd = false)
        {
            base.SetPosition(position.Offset);
            Indent = 0;
            if (position.Line.Length < TabString.Length)
            {
                return;
            }

            var index = 0;
            while (position.Line.Length - index >= TabString.Length &&
                position.Line
                    .Substring(index, TabString.Length)
                    .Equals(TabString, StringComparison.Ordinal))
            {
                index = index + TabString.Length;
                Indent++;
            }
        }

        internal void WriteEnum<TEnum>(TEnum value)
            where TEnum : struct
        {
            WriteTypeExpression(typeof(TEnum));
            string stringValue = value.ToString();
            if (!stringValue.Contains(Symbols.Comma))
            {
                Write(StaticOperator);
                Write(value);
                return;
            }

            WriteStringExpression(StringConstantType.SingleQuoted, stringValue);
        }

        internal MethodChainWriter StartMethodChain()
        {
            return new MethodChainWriter(this);
        }

        internal void WriteExplicitMethodParameterName(string memberName)
        {
            WriteChars(LessThan, NumberSign, Space);
            Write(memberName);
            WriteChars(Colon, Space, NumberSign, GreaterThan, Space);
        }

        internal void WriteAssignment(Action rhs, Action lhs)
        {
            rhs();
            WriteChars(Space, Equal, Space);
            lhs();
        }

        internal void WriteVariable(string variableName, bool isSplat = false)
        {
            if (isSplat)
            {
                Write(At);
            }
            else
            {
                Write(Dollar);
            }

            bool shouldEscape = variableName.IndexOfAny(s_invalidVariableNameChars) != -1;
            if (shouldEscape)
            {
                Write(CurlyOpen);
            }

            Write(variableName);

            if (shouldEscape)
            {
                Write(CurlyClose);
            }
        }

        internal void WriteComment(string text, int maxLineLength)
        {
            WriteChars(NumberSign, Space);
            maxLineLength = maxLineLength - Indent - 2;
            if (text.Length <= maxLineLength)
            {
                Write(text);
                return;
            }

            WriteWrappedLines(
                text,
                maxLineLength - Indent - 2,
                () =>
                {
                    WriteLine();
                    WriteChars(NumberSign, Space);
                });
        }

        internal void WriteMemberDefinition(MemberDescription member)
        {
            if (member.IsStatic)
            {
                Write(Static);
                Write(Space);
            }

            if (member.MemberType != MemberTypes.Constructor)
            {
                WriteTypeExpression(member.ReturnType);
                Write(Space);
            }

            if (member.MemberType == MemberTypes.Property)
            {
                Write(Dollar);
            }

            Write(member.Name);
            if (member.MemberType == MemberTypes.Property)
            {
                Write(Semi);
                return;
            }

            Write('(');
            WriteEachWithSeparator(
                member.Parameters.ToArray(),
                parameter =>
                {
                    WriteTypeExpression(parameter.ParameterType);
                    Write(new[] { Space, Dollar });
                    Write(parameter.Name);
                },
                MethodParameterSeparator);
            Write(new[] { ParenClose, Space });
            OpenScriptBlock();
            Write(Throw);
            Write(Space);
            WriteConstructor(new PSTypeName(typeof(NotImplementedException)));
            CloseScriptBlock();
        }

        internal void WriteConstructor(Type type)
        {
            WriteConstructor(type, argumentWriter: null);
        }

        internal void WriteConstructor(Type type, Action argumentWriter)
        {
            WriteStaticMethodInvocation(new PSTypeName(type), New, argumentWriter);
        }

        internal void WriteConstructor(PSTypeName type)
        {
            WriteStaticMemberExpression(type, New, isMethod: true);
        }

        internal void WriteConstructor(PSTypeName type, Action argumentWriter)
        {
            WriteStaticMethodInvocation(type, New, argumentWriter);
        }

        internal void WriteStaticMemberExpression(Type type, string memberName, bool isMethod = false)
        {
            WriteStaticMemberExpression(
                new PSTypeName(type),
                memberName,
                isMethod);
        }

        internal void WriteStaticMemberExpression(PSTypeName type, string memberName, bool isMethod = false)
        {
            WriteStaticMemberExpression(type, memberName.ToCharArray(), isMethod);
        }

        internal void WriteStaticMemberExpression(PSTypeName type, char[] memberName, bool isMethod = false)
        {
            if (isMethod)
            {
                WriteStaticMethodInvocation(type, memberName, null);
                return;
            }

            WriteStaticPropertyExpression(type, memberName);
        }

        internal void WriteStaticPropertyExpression(PSTypeName type, char[] memberName)
        {
            WriteTypeExpression(type);
            Write(StaticOperator);
            Write(memberName);
        }

        internal void WriteStaticMethodInvocation(PSTypeName type, char[] memberName, Action argumentWriter)
        {
            WriteTypeExpression(type);
            Write(StaticOperator);
            Write(memberName);
            Write(ParenOpen);
            argumentWriter?.Invoke();
            Write(ParenClose);
        }

        internal void WriteStaticMemberExpression(PSTypeName type, string memberName, Action argumentWriter)
        {
            WriteTypeExpression(type);
            Write(StaticOperator);
            Write(memberName);
            Write(ParenOpen);
            argumentWriter?.Invoke();
            Write(ParenClose);
        }

        internal void OpenScriptBlock()
        {
            Write(CurlyOpen);
            FrameOpen();
        }

        internal void CloseScriptBlock()
        {
            FrameClose();
            Write(CurlyClose);
        }

        internal void OpenHashtable()
        {
            Write(HashtableOpen);
            FrameOpen();
        }

        internal void CloseHashtable()
        {
            FrameClose();
            Write(HashtableClose);
        }

        internal void WriteHashtableEntry(string key, Action valueWriter)
        {
            Write(key);
            Write(new[] { Space, Equal, Space });
            valueWriter();
        }

        internal void WriteHashtableEntry(string key, Action valueWriter, int aligner)
        {
            var sb = new StringBuilder(key);
            sb.Append(
                Symbols.Space,
                aligner - key.Length);
            this.WriteHashtableEntry(
                sb.ToString(),
                valueWriter);
        }

        internal void WriteStringExpression(StringConstantType type, string value)
        {
            char quoteChar;
            if (type == StringConstantType.DoubleQuoted)
            {
                quoteChar = DoubleQuote;
            }
            else if (type == StringConstantType.SingleQuoted)
            {
                quoteChar = SingleQuote;
            }
            else
            {
                Write(value);
                return;
            }

            Write(quoteChar);
            Write(value);
            Write(quoteChar);
        }

        internal void WriteTypeExpression(
            Type type,
            bool writeBrackets = true,
            bool forAttribute = false,
            bool allowNonPublic = false,
            bool skipGenericArgs = false)
        {
            WriteTypeExpression(
                new PSTypeName(type),
                writeBrackets,
                forAttribute,
                allowNonPublic,
                skipGenericArgs);
        }

        internal void WriteTypeExpression(
            PSTypeName type,
            bool writeBrackets = true,
            bool forAttribute = false,
            bool allowNonPublic = false,
            bool skipGenericArgs = false)
        {
            if (type.Type != null && !MemberUtil.IsTypeExpressible(type.Type) && allowNonPublic)
            {
                WriteReflectionTypeExpression(type.Type);
                return;
            }

            if (writeBrackets)
            {
                Write(SquareOpen);
            }

            if (type.Type != null)
            {
                Write(
                    MemberUtil.GetTypeNameForLiteral(
                        type.Type,
                        dropNamespaces: ShouldDropNamespaces,
                        out string[] droppedNamespaces,
                        forAttribute,
                        skipGenericArgs));

                foreach (string ns in droppedNamespaces)
                {
                    _pendingUsingWrites.Add(ns);
                }
            }
            else
            {
                Write(type.Name);
            }

            if (writeBrackets)
            {
                Write(SquareClose);
            }
        }

        internal void WriteReflectionTypeExpression(Type type)
        {
            if (type.IsGenericType && MemberUtil.IsTypeExpressible(type, shouldSkipGenericArgs: true))
            {
                WriteTypeExpression(new PSTypeName(type), skipGenericArgs: true);
            }
            else
            {
                Write(SquareOpen);
                Write(MemberUtil.GetShortestExpressibleTypeName(type, out string droppedNamespace));
                if (!string.IsNullOrEmpty(droppedNamespace))
                {
                    _pendingUsingWrites.Add(droppedNamespace);
                }

                WriteChars(SquareClose, Dot);
                Write("Assembly");
                Write(Dot);
                Write("GetType");
                Write(ParenOpen);
                WriteStringExpression(StringConstantType.SingleQuoted, type.ToString());
                Write(ParenClose);
            }

            if (!type.IsGenericType)
            {
                return;
            }

            Write(Dot);
            Write("MakeGenericType");
            Write(ParenOpen);
            WriteEachWithSeparator(
                type.GetGenericArguments(),
                t => WriteTypeExpression(new PSTypeName(t), allowNonPublic: true),
                () => WriteChars(Comma, Dot));
            Write(ParenClose);
        }

        internal void WriteAttributeStatement(PSTypeName typeName)
        {
            OpenAttributeStatement(typeName);
            CloseAttributeStatement();
        }

        internal void OpenAttributeStatement(PSTypeName typeName)
        {
            Write(SquareOpen);
            WriteTypeExpression(
                typeName,
                writeBrackets: false,
                forAttribute: true);
            Write(ParenOpen);
        }

        internal void CloseAttributeStatement()
        {
            Write(new[] { ParenClose, SquareClose });
        }

        internal void WriteAttributeNamedArgument(string key, string value)
        {
            Write(key);
            Write(new[] { Space, Equal, Space });
            WriteStringExpression(
                StringConstantType.SingleQuoted,
                value);
        }

        internal void WriteAttributeNamedArgument(string key, Action valueWriter)
        {
            Write(key);
            Write(new[] { Space, Equal, Space });
            valueWriter();
        }

        internal void WriteParamBlock()
        {
            OpenParamBlock();
            CloseParamBlock();
        }

        internal void OpenParamBlock(bool shouldPushIndent = false)
        {
            Write(Param);
            Write(ParenOpen);
            if (!shouldPushIndent)
            {
                return;
            }

            FrameOpen();
        }

        internal void CloseParamBlock(bool shouldPopIndent = false)
        {
            if (shouldPopIndent)
            {
                FrameClose();
            }

            Write(ParenClose);
        }

        internal void WriteUsingStatement(string name, UsingStatementKind kind)
        {
            char[] kindSymbol;
            switch (kind)
            {
                case UsingStatementKind.Assembly:
                    kindSymbol = Symbols.Assembly;
                    break;
                case UsingStatementKind.Module:
                    kindSymbol = Symbols.Module;
                    break;
                case UsingStatementKind.Namespace:
                    kindSymbol = Symbols.Namespace;
                    break;
                default:
                    throw new InvalidOperationException();
            }

            Write(Using);
            Write(Space);
            Write(kindSymbol);
            Write(name);
        }

        private static string GetDocumentText(Ast ast)
        {
            Validate.IsNotNull(nameof(ast), ast);
            return ast.Extent.Text;
        }
    }
}
