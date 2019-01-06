using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
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

        private readonly HashSet<string> _pendingUsingWrites = new HashSet<string>();

        private ScriptBlockAst _ast;

        private DocumentContextBase _context;

        private bool? _shouldDropNamespaces;

        public PowerShellScriptWriter()
            : base()
        {
            _ast = Empty.ScriptAst.Untitled;
        }

        public PowerShellScriptWriter(string initialValue)
            : this(initialValue, string.Empty)
        {
        }

        public PowerShellScriptWriter(string initialValue, string fileName)
            : base(initialValue, fileName)
        {
            if (string.IsNullOrEmpty(initialValue))
            {
                _ast = Empty.ScriptAst.Untitled;
                return;
            }

            _ast = Parser.ParseInput(initialValue, out _, out _);
        }

        public PowerShellScriptWriter(Ast ast)
            : this(ast, string.Empty)
        {
        }

        public PowerShellScriptWriter(Ast ast, string fileName)
            : base(GetDocumentText(ast.FindRootAst()), fileName)
        {
            _ast = ast.FindRootAst();
        }

        public PowerShellScriptWriter(DocumentContextBase context)
            : this(context, string.Empty)
        {
        }

        public PowerShellScriptWriter(DocumentContextBase context, string fileName)
            : base(GetDocumentText(context.RootAst), fileName)
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

        internal void OpenFunctionDefinition(string name)
        {
            Write(Function);
            Write(Space);
            Write(name);
            Write(Space);
            OpenScriptBlock();
        }

        internal void CloseFunctionDefinition() => CloseScriptBlock();

        internal void OpenNamedBlock(TokenKind blockKind)
        {
            switch (blockKind)
            {
                case TokenKind.End: Write(Symbols.End); break;
                case TokenKind.Begin: Write(Symbols.Begin); break;
                case TokenKind.Process: Write(Symbols.Process); break;
                default: throw new ArgumentException(blockKind.ToString(), nameof(blockKind));
            }

            Write(Symbols.Space);
            OpenScriptBlock();
        }

        internal void CloseNamedBlock() => CloseScriptBlock();

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

        internal void WriteVariable(string variableName, CaseType? caseType = null, bool isSplat = false)
        {
            WriteVariable(variableName.AsSpan(), caseType, isSplat);
        }

        internal void WriteVariable(ReadOnlySpan<char> variableName, CaseType? caseType = null, bool isSplat = false)
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

            if (caseType != null)
            {
                WriteCasedString(variableName, caseType.Value);
            }
            else
            {
                Write(variableName);
            }

            if (shouldEscape)
            {
                Write(CurlyClose);
            }
        }

        internal void WriteCasedString(ReadOnlySpan<char> value, CaseType caseType)
        {
            if (caseType == CaseType.CamelCase)
            {
                Write(char.ToLowerInvariant(value[0]));
            }
            else
            {
                Write(char.ToUpperInvariant(value[0]));
            }

            if (value.Length == 1)
            {
                return;
            }

            Write(value.Slice(1));
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
            this.WriteEachWithSeparator(
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
            bool skipGenericArgs = false,
            bool strictTypes = false)
        {
            Type reflectionType = type.Type ?? typeof(object);
            if (!MemberUtil.IsTypeExpressible(reflectionType))
            {
                if (allowNonPublic)
                {
                    WriteReflectionTypeExpression(reflectionType);
                    return;
                }

                if (strictTypes)
                {
                    throw new PSArgumentException(nameof(type));
                }

                // If the type is public but not expressible, then it's probably a generic type
                // with non-public type arguments. We can't roll back non-public type arguments
                // without affecting anything, so just call it an Object.
                if (reflectionType.IsPublic)
                {
                    reflectionType = typeof(object);
                }
                else
                {
                    do
                    {
                        if (reflectionType.BaseType == null)
                        {
                            reflectionType = typeof(object);
                            break;
                        }

                        reflectionType = reflectionType.BaseType;
                    }
                    while (!MemberUtil.IsTypeExpressible(reflectionType));
                }
            }

            if (writeBrackets)
            {
                Write(SquareOpen);
            }

            if (type.Type != null)
            {
                Write(
                    MemberUtil.GetTypeNameForLiteral(
                        reflectionType,
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

        internal void WriteAsExpressionValue(Parameter parameter, bool shouldNotWriteHints)
        {
            if (parameter.Value == null)
            {
                WriteSplatParameterHint(parameter, shouldNotWriteHints);
                return;
            }

            if (parameter.Value.ConstantValue is bool boolean)
            {
                Write(Dollar + boolean.ToString().ToLower());
                return;
            }

            var commandElementConverter = new ConvertFromCommandElementWriter(this);
            parameter.Value.Value.Visit(commandElementConverter);
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
            WriteChars(ParenClose, SquareClose);
        }

        internal void WriteAttributeNamedArgument(string key, string value)
        {
            Write(key);
            WriteChars(Space, Equal, Space);
            WriteStringExpression(
                StringConstantType.SingleQuoted,
                value);
        }

        internal void WriteAttributeNamedArgument(string key, Action valueWriter)
        {
            Write(key);
            WriteChars(Space, Equal, Space);
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

        private void WriteSplatParameterHint(Parameter parameter, bool shouldNotWriteHints)
        {
            if (shouldNotWriteHints)
            {
                WriteVariable(parameter.Name, CaseType.CamelCase);
                return;
            }

            Write(Dollar);
            CaseType caseType;
            if (parameter.IsMandatory)
            {
                Write("mandatory");
                caseType = CaseType.PascalCase;
            }
            else
            {
                caseType = CaseType.CamelCase;
            }

            Type parameterType = parameter.Type ?? typeof(object);

            // Unwrap array, byref, and pointer types. The latter two are pretty unlikely
            // for a command parameter, but we already need to take care of arrays anyway.
            while (parameterType.HasElementType)
            {
                parameterType = parameterType.GetElementType();
            }

            ReadOnlySpan<char> typeName = parameterType.Name.AsSpan();
            int backTickIndex = typeName.IndexOf(Symbols.Backtick);
            if (backTickIndex != -1)
            {
                typeName = typeName.Slice(0, backTickIndex);
            }

            WriteCasedString(typeName, caseType);
            if (parameter.Type.IsArray)
            {
                Write("Array");
            }

            Write(parameter.Name);
        }

        private class ConvertFromCommandElementWriter : AstVisitor
        {
            private readonly PowerShellScriptWriter _writer;

            internal ConvertFromCommandElementWriter(PowerShellScriptWriter writer)
            {
                _writer = writer;
            }

            public override AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst)
            {
                if (expandableStringExpressionAst.StringConstantType == StringConstantType.BareWord)
                {
                    _writer.WriteStringExpression(
                        StringConstantType.DoubleQuoted,
                        expandableStringExpressionAst.Extent.Text);
                    return AstVisitAction.SkipChildren;
                }

                return WriteExtentAndSkip(expandableStringExpressionAst);
            }

            public override AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
            {
                if (stringConstantExpressionAst.StringConstantType == StringConstantType.BareWord)
                {
                    _writer.WriteStringExpression(
                        StringConstantType.SingleQuoted,
                        stringConstantExpressionAst.Extent.Text);
                    return AstVisitAction.Continue;
                }

                return WriteExtentAndSkip(stringConstantExpressionAst);
            }

            public override AstVisitAction VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
            {
                _writer.WriteEachWithSeparator(
                    arrayLiteralAst.Elements,
                    element => element.Visit(this),
                    () => _writer.WriteChars(Comma, Space));
                return AstVisitAction.SkipChildren;
            }

            public override AstVisitAction VisitHashtable(HashtableAst hashtableAst)
                => WriteExtentAndSkip(hashtableAst);

            public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
                => WriteExtentAndSkip(scriptBlockExpressionAst);

            public override AstVisitAction VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
                => WriteExtentAndSkip(arrayExpressionAst);

            public override AstVisitAction VisitParenExpression(ParenExpressionAst parenExpressionAst)
                => WriteExtentAndSkip(parenExpressionAst);

            public override AstVisitAction VisitSubExpression(SubExpressionAst subExpressionAst)
                => WriteExtentAndSkip(subExpressionAst);

            public override AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst)
                => WriteExtentAndSkip(variableExpressionAst, skipNewLineCheck: true);

            public override AstVisitAction VisitMemberExpression(MemberExpressionAst memberExpressionAst)
                => WriteExtentAndSkip(memberExpressionAst, skipNewLineCheck: true);

            public override AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst methodCallAst)
                => WriteExtentAndSkip(methodCallAst);

            public override AstVisitAction VisitUsingExpression(UsingExpressionAst usingExpressionAst)
                => WriteExtentAndSkip(usingExpressionAst, skipNewLineCheck: true);

            public override AstVisitAction VisitIndexExpression(IndexExpressionAst indexExpressionAst)
                => WriteExtentAndSkip(indexExpressionAst);

            public override AstVisitAction VisitConstantExpression(ConstantExpressionAst constantExpressionAst)
                => WriteExtentAndSkip(constantExpressionAst, skipNewLineCheck: true);

            private AstVisitAction WriteExtentAndSkip(Ast ast, bool skipNewLineCheck = false)
            {
                if (skipNewLineCheck)
                {
                    _writer.Write(ast.Extent.Text);
                    return AstVisitAction.SkipChildren;
                }

                TextUtilities.ForEachIndentNormalizedLine(
                    line => _writer.Write(line),
                    () => _writer.WriteLine(),
                    ast.Extent.Text,
                    _writer.NewLine,
                    _writer.TabString,
                    ignoreFirstLine: true);

                return AstVisitAction.SkipChildren;
            }
        }
    }
}
