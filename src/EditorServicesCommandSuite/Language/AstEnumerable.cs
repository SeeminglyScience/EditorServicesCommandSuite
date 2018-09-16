using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    /// <summary>
    /// Provides the ability to find multiple ASTs similar to <see cref="Ast.FindAll(Func{Ast, bool}, bool)" />
    /// but without creating an array for the result. Using this runs a tad bit slower in larger
    /// ASTs than the traditional method but it generates significantly less allocations thanks
    /// to utilizing an <see cref="ArrayPool{T}" /> internally.
    /// </summary>
    internal class AstEnumerable : IEnumerable<Ast>
    {
        private const int MinimumDefaultBuffer = 1024;

        private const int ApproximateCharactersPerAst = 16;

        private const int LargeDocumentThreshold = MinimumDefaultBuffer * ApproximateCharactersPerAst;

        private const int ApproximateAstPerTreeLevel = 4;

        private const int MinimumDefaultLevelBuffer = MinimumDefaultBuffer / ApproximateAstPerTreeLevel;

        private readonly Ast _startingAst;

        private readonly bool _excludeNestedScriptBlocks;

        private readonly bool _excludeSelf;

        private readonly int _bufferSize;

        private readonly Func<Ast, bool> _predicate;

        private AstEnumerable(
            Ast startingAst,
            Func<Ast, bool> predicate,
            int bufferSize,
            bool excludeNestedScriptBlocks,
            bool excludeSelf)
        {
            _startingAst = startingAst;
            _predicate = predicate;
            _bufferSize = bufferSize;
            _excludeNestedScriptBlocks = excludeNestedScriptBlocks;
            _excludeSelf = excludeSelf;
        }

        public static IEnumerable<Ast> Create(
            Ast startingAst,
            bool excludeNestedScriptBlocks = false,
            bool excludeSelf = false)
        {
            return new AstEnumerable(
                startingAst,
                predicate: null,
                GetBufferSize(startingAst),
                excludeNestedScriptBlocks,
                excludeSelf);
        }

        public static IEnumerable<Ast> Create(
            Ast startingAst,
            Func<Ast, bool> predicate,
            bool excludeNestedScriptBlocks = false,
            bool excludeSelf = false)
        {
            return new AstEnumerable(
                startingAst,
                predicate,
                GetBufferSize(startingAst),
                excludeNestedScriptBlocks,
                excludeSelf);
        }

        public static IEnumerable<Ast> Create(
            Ast startingAst,
            Func<Ast, bool> predicate,
            int bufferSize,
            bool excludeNestedScriptBlocks = false,
            bool excludeSelf = false)
        {
            if (bufferSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            return new AstEnumerable(
                startingAst,
                predicate,
                bufferSize,
                excludeNestedScriptBlocks,
                excludeSelf);
        }

        public IEnumerator<Ast> GetEnumerator()
        {
            return new AstEnumerator(
                _startingAst,
                _predicate,
                _bufferSize,
                !_excludeNestedScriptBlocks,
                _excludeSelf);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new AstEnumerator(
                _startingAst,
                _predicate,
                _bufferSize,
                !_excludeNestedScriptBlocks,
                _excludeSelf);
        }

        private static int GetBufferSize(Ast ast)
        {
            if (ast == null)
            {
                return MinimumDefaultBuffer;
            }

            // Some quick checks showed an average of one ast per ~16 characters for larger
            // documents. A buffer of 1024 should be more than enough for most things though.
            if (ast.Extent.Text.Length > LargeDocumentThreshold)
            {
                return ast.Extent.Text.Length / ApproximateCharactersPerAst;
            }

            return MinimumDefaultBuffer;
        }

        [DebuggerDisplay("C: {Current} M: {Max} R: {CurrentWasRecursed}")]
        private readonly struct TreeLevel
        {
            internal readonly int Current;

            internal readonly int Max;

            internal readonly bool CurrentWasRecursed;

            internal TreeLevel(int current, int max, bool currentWasRecursed)
            {
                Current = current;
                Max = max;
                CurrentWasRecursed = currentWasRecursed;
            }
        }

        private class AstEnumerator : IEnumerator<Ast>, ICustomAstVisitor, ICustomAstVisitor2
        {
            private const int Completed = -2;

            private const int NotStarted = -1;

            private readonly bool _searchNestedScriptBlocks;

            private readonly bool _excludeSelf;

            private readonly Func<Ast, bool> _predicate;

            private Ast[] _buffer;

            private int _bufferIndex;

            private TreeLevel[] _treeLevelStack;

            private int _treeLevelIndex = NotStarted;

            private bool _isDisposed;

            public AstEnumerator(
                Ast startingAst,
                Func<Ast, bool> predicate,
                int bufferSize,
                bool searchNestedScriptBlocks,
                bool excludeSelf)
            {
                _predicate = predicate;
                _searchNestedScriptBlocks = searchNestedScriptBlocks;
                _excludeSelf = excludeSelf;
                _treeLevelStack = ArrayPool<TreeLevel>.Shared.Rent(
                    bufferSize / ApproximateAstPerTreeLevel);

                _buffer = ArrayPool<Ast>.Shared.Rent(bufferSize);
                _buffer[0] = startingAst;
            }

            public Ast Current => _treeLevelIndex < 0
                ? null
                : _buffer[_treeLevelStack[_treeLevelIndex].Current];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                Dispose(true);
            }

            public bool MoveNext()
            {
                if (_treeLevelIndex == Completed)
                {
                    return false;
                }

                if (_treeLevelIndex == NotStarted)
                {
                    if (_buffer[0] == null)
                    {
                        _treeLevelIndex = Completed;
                        return false;
                    }

                    _treeLevelStack[0] = default;
                    _treeLevelIndex++;

                    if (_predicate == null || _predicate(_buffer[0]))
                    {
                        return true;
                    }

                    return MoveNext();
                }

                bool foundNext = false;
                bool shouldPopLevel = false;
                while (!foundNext)
                {
                    if (shouldPopLevel)
                    {
                        shouldPopLevel = false;
                        if (_treeLevelIndex <= 0)
                        {
                            _treeLevelIndex = Completed;
                            return false;
                        }

                        _treeLevelIndex--;
                    }

                    TreeLevel treeLevel = _treeLevelStack[_treeLevelIndex];
                    if (!treeLevel.CurrentWasRecursed)
                    {
                        int currentBufferIndex = _bufferIndex;
                        _buffer[treeLevel.Current].Visit(this);
                        if (_bufferIndex != currentBufferIndex)
                        {
                            _treeLevelStack[_treeLevelIndex] = new TreeLevel(
                                treeLevel.Current,
                                treeLevel.Max,
                                currentWasRecursed: true);

                            TreeLevel newTreeLevel = new TreeLevel(
                                currentBufferIndex + 1,
                                _bufferIndex,
                                currentWasRecursed: false);

                            EnsureCapacity(ref _treeLevelStack, _treeLevelIndex, 1);
                            _treeLevelIndex++;
                            _treeLevelStack[_treeLevelIndex] = newTreeLevel;
                            foundNext = _predicate == null || _predicate(_buffer[newTreeLevel.Current]);
                            continue;
                        }
                    }

                    if (treeLevel.Current == treeLevel.Max)
                    {
                        shouldPopLevel = true;
                        continue;
                    }

                    _treeLevelStack[_treeLevelIndex] = new TreeLevel(
                        treeLevel.Current + 1,
                        treeLevel.Max,
                        currentWasRecursed: false);

                    foundNext = _predicate == null || _predicate(_buffer[treeLevel.Current + 1]);
                }

                return true;
            }

            public void Reset()
            {
                if (_isDisposed)
                {
                    return;
                }

                _treeLevelIndex = NotStarted;
                _bufferIndex = 0;
            }

            protected virtual void Dispose(bool disposing)
            {
                if (_isDisposed)
                {
                    return;
                }

                if (disposing)
                {
                    ArrayPool<Ast>.Shared.Return(_buffer);
                    ArrayPool<TreeLevel>.Shared.Return(_treeLevelStack);
                    _buffer = null;
                    _treeLevelStack = null;
                    _treeLevelIndex = Completed;
                }

                _isDisposed = true;
            }

            private void SafeAdd(Ast ast)
            {
                if (ast == null)
                {
                    return;
                }

                Add(ast);
            }

            private void Add(Ast ast)
            {
                EnsureCapacity(ref _buffer, _bufferIndex, 1);
                _bufferIndex++;
                _buffer[_bufferIndex] = ast;
            }

            private void SafeAddRange<TAst>(IList<TAst> asts)
                where TAst : Ast
            {
                if (asts == null)
                {
                    return;
                }

                AddRange(asts);
            }

            private void AddRange<TAst>(IList<TAst> asts)
                where TAst : Ast
            {
                EnsureCapacity(ref _buffer, _bufferIndex, asts.Count);
                for (var i = 0; i < asts.Count; i++)
                {
                    _buffer[_bufferIndex + i + 1] = asts[i];
                }

                _bufferIndex += asts.Count;
            }

            private void EnsureCapacity<T>(ref T[] array, int index, int count)
            {
                if (array.Length > index + count)
                {
                    return;
                }

                T[] newBuffer = ArrayPool<T>.Shared.Rent(array.Length * 2);
                array.CopyTo(newBuffer, 0);
                ArrayPool<T>.Shared.Return(array);
                array = newBuffer;
            }

#pragma warning disable SA1202
            public object VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
            {
                Add(arrayExpressionAst.SubExpression);
                return null;
            }

            public object VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
            {
                AddRange(arrayLiteralAst.Elements);
                return null;
            }

            public object VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
            {
                Add(assignmentStatementAst.Left);
                Add(assignmentStatementAst.Right);
                return null;
            }

            public object VisitAttribute(AttributeAst attributeAst)
            {
                AddRange(attributeAst.NamedArguments);
                AddRange(attributeAst.PositionalArguments);
                return null;
            }

            public object VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst)
            {
                Add(attributedExpressionAst.Attribute);
                Add(attributedExpressionAst.Child);
                return null;
            }

            public object VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst)
            {
                Add(binaryExpressionAst.Left);
                Add(binaryExpressionAst.Right);
                return null;
            }

            public object VisitBlockStatement(BlockStatementAst blockStatementAst)
            {
                Add(blockStatementAst.Body);
                return null;
            }

            public object VisitBreakStatement(BreakStatementAst breakStatementAst)
            {
                SafeAdd(breakStatementAst.Label);
                return null;
            }

            public object VisitCatchClause(CatchClauseAst catchClauseAst)
            {
                AddRange(catchClauseAst.CatchTypes);
                Add(catchClauseAst.Body);
                return null;
            }

            public object VisitCommand(CommandAst commandAst)
            {
                AddRange(commandAst.CommandElements);
                AddRange(commandAst.Redirections);
                return null;
            }

            public object VisitCommandExpression(CommandExpressionAst commandExpressionAst)
            {
                Add(commandExpressionAst.Expression);
                SafeAddRange(commandExpressionAst.Redirections);
                return null;
            }

            public object VisitCommandParameter(CommandParameterAst commandParameterAst)
            {
                SafeAdd(commandParameterAst.Argument);
                return null;
            }

            public object VisitConstantExpression(ConstantExpressionAst constantExpressionAst)
            {
                return null;
            }

            public object VisitContinueStatement(ContinueStatementAst continueStatementAst)
            {
                SafeAdd(continueStatementAst.Label);
                return null;
            }

            public object VisitConvertExpression(ConvertExpressionAst convertExpressionAst)
            {
                Add(convertExpressionAst.Attribute);
                Add(convertExpressionAst.Child);
                return null;
            }

            public object VisitDataStatement(DataStatementAst dataStatementAst)
            {
                AddRange(dataStatementAst.CommandsAllowed);
                Add(dataStatementAst.Body);
                return null;
            }

            public object VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst)
            {
                Add(doUntilStatementAst.Body);
                Add(doUntilStatementAst.Condition);
                return null;
            }

            public object VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst)
            {
                Add(doWhileStatementAst.Body);
                Add(doWhileStatementAst.Condition);
                return null;
            }

            public object VisitErrorExpression(ErrorExpressionAst errorExpressionAst)
            {
                AddRange(errorExpressionAst.NestedAst);
                return null;
            }

            public object VisitErrorStatement(ErrorStatementAst errorStatementAst)
            {
                AddRange(errorStatementAst.Conditions);
                AddRange(errorStatementAst.Bodies);
                AddRange(errorStatementAst.NestedAst);
                return null;
            }

            public object VisitExitStatement(ExitStatementAst exitStatementAst)
            {
                SafeAdd(exitStatementAst.Pipeline);
                return null;
            }

            public object VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst)
            {
                SafeAddRange(expandableStringExpressionAst.NestedExpressions);
                return null;
            }

            public object VisitFileRedirection(FileRedirectionAst fileRedirectionAst)
            {
                SafeAdd(fileRedirectionAst.Location);
                return null;
            }

            public object VisitForEachStatement(ForEachStatementAst forEachStatementAst)
            {
                Add(forEachStatementAst.Variable);
                Add(forEachStatementAst.Condition);
                Add(forEachStatementAst.Body);
                return null;
            }

            public object VisitForStatement(ForStatementAst forStatementAst)
            {
                Add(forStatementAst.Initializer);
                Add(forStatementAst.Condition);
                SafeAdd(forStatementAst.Iterator);
                Add(forStatementAst.Body);
                return null;
            }

            public object VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst)
            {
                SafeAddRange(functionDefinitionAst.Parameters);
                Add(functionDefinitionAst.Body);
                return null;
            }

            public object VisitHashtable(HashtableAst hashtableAst)
            {
                foreach (Tuple<ExpressionAst, StatementAst> pair in hashtableAst.KeyValuePairs)
                {
                    Add(pair.Item1);
                    Add(pair.Item2);
                }

                return null;
            }

            public object VisitIfStatement(IfStatementAst ifStmtAst)
            {
                foreach (Tuple<PipelineBaseAst, StatementBlockAst> pair in ifStmtAst.Clauses)
                {
                    Add(pair.Item1);
                    Add(pair.Item2);
                }

                SafeAdd(ifStmtAst.ElseClause);
                return null;
            }

            public object VisitIndexExpression(IndexExpressionAst indexExpressionAst)
            {
                Add(indexExpressionAst.Target);
                Add(indexExpressionAst.Index);
                return null;
            }

            public object VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst)
            {
                Add(invokeMemberExpressionAst.Expression);
                Add(invokeMemberExpressionAst.Member);
                SafeAddRange(invokeMemberExpressionAst.Arguments);
                return null;
            }

            public object VisitMemberExpression(MemberExpressionAst memberExpressionAst)
            {
                Add(memberExpressionAst.Expression);
                Add(memberExpressionAst.Member);
                return null;
            }

            public object VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst)
            {
                return null;
            }

            public object VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst)
            {
                SafeAdd(namedAttributeArgumentAst.Argument);
                return null;
            }

            public object VisitNamedBlock(NamedBlockAst namedBlockAst)
            {
                SafeAddRange(namedBlockAst.Traps);
                AddRange(namedBlockAst.Statements);
                return null;
            }

            public object VisitParamBlock(ParamBlockAst paramBlockAst)
            {
                SafeAddRange(paramBlockAst.Attributes);
                AddRange(paramBlockAst.Parameters);
                return null;
            }

            public object VisitParameter(ParameterAst parameterAst)
            {
                SafeAddRange(parameterAst.Attributes);
                Add(parameterAst.Name);
                SafeAdd(parameterAst.DefaultValue);
                return null;
            }

            public object VisitParenExpression(ParenExpressionAst parenExpressionAst)
            {
                Add(parenExpressionAst.Pipeline);
                return null;
            }

            public object VisitPipeline(PipelineAst pipelineAst)
            {
                AddRange(pipelineAst.PipelineElements);
                return null;
            }

            public object VisitReturnStatement(ReturnStatementAst returnStatementAst)
            {
                SafeAdd(returnStatementAst.Pipeline);
                return null;
            }

            public object VisitScriptBlock(ScriptBlockAst scriptBlockAst)
            {
                if (!_searchNestedScriptBlocks)
                {
                    return null;
                }

                SafeAddRange(scriptBlockAst.UsingStatements);
                SafeAddRange(scriptBlockAst.Attributes);
                SafeAdd(scriptBlockAst.ParamBlock);
                SafeAdd(scriptBlockAst.DynamicParamBlock);
                SafeAdd(scriptBlockAst.BeginBlock);
                SafeAdd(scriptBlockAst.ProcessBlock);
                SafeAdd(scriptBlockAst.EndBlock);
                return null;
            }

            public object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
            {
                Add(scriptBlockExpressionAst.ScriptBlock);
                return null;
            }

            public object VisitStatementBlock(StatementBlockAst statementBlockAst)
            {
                SafeAddRange(statementBlockAst.Traps);
                AddRange(statementBlockAst.Statements);
                return null;
            }

            public object VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
            {
                return null;
            }

            public object VisitSubExpression(SubExpressionAst subExpressionAst)
            {
                Add(subExpressionAst.SubExpression);
                return null;
            }

            public object VisitSwitchStatement(SwitchStatementAst switchStatementAst)
            {
                Add(switchStatementAst.Condition);
                foreach (Tuple<ExpressionAst, StatementBlockAst> pair in switchStatementAst.Clauses)
                {
                    Add(pair.Item1);
                    Add(pair.Item2);
                }

                SafeAdd(switchStatementAst.Default);
                return null;
            }

            public object VisitThrowStatement(ThrowStatementAst throwStatementAst)
            {
                SafeAdd(throwStatementAst.Pipeline);
                return null;
            }

            public object VisitTrap(TrapStatementAst trapStatementAst)
            {
                SafeAdd(trapStatementAst.TrapType);
                SafeAdd(trapStatementAst.Body);
                return null;
            }

            public object VisitTryStatement(TryStatementAst tryStatementAst)
            {
                Add(tryStatementAst.Body);
                SafeAddRange(tryStatementAst.CatchClauses);
                SafeAdd(tryStatementAst.Finally);
                return null;
            }

            public object VisitTypeConstraint(TypeConstraintAst typeConstraintAst)
            {
                return null;
            }

            public object VisitTypeExpression(TypeExpressionAst typeExpressionAst)
            {
                return null;
            }

            public object VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst)
            {
                Add(unaryExpressionAst.Child);
                return null;
            }

            public object VisitUsingExpression(UsingExpressionAst usingExpressionAst)
            {
                SafeAdd(usingExpressionAst.SubExpression);
                return null;
            }

            public object VisitVariableExpression(VariableExpressionAst variableExpressionAst)
            {
                return null;
            }

            public object VisitWhileStatement(WhileStatementAst whileStatementAst)
            {
                Add(whileStatementAst.Condition);
                Add(whileStatementAst.Body);
                return null;
            }

            public object VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst)
            {
                SafeAdd(baseCtorInvokeMemberExpressionAst.Expression);
                SafeAddRange(baseCtorInvokeMemberExpressionAst.Arguments);
                return null;
            }

            public object VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst)
            {
                Add(configurationDefinitionAst.InstanceName);
                Add(configurationDefinitionAst.Body);
                return null;
            }

            public object VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordAst)
            {
                SafeAddRange(dynamicKeywordAst.CommandElements);
                return null;
            }

            public object VisitFunctionMember(FunctionMemberAst functionMemberAst)
            {
                SafeAddRange(functionMemberAst.Attributes);
                Add(functionMemberAst.ReturnType);
                SafeAddRange(functionMemberAst.Parameters);
                Add(functionMemberAst.Body);
                return null;
            }

            public object VisitPropertyMember(PropertyMemberAst propertyMemberAst)
            {
                SafeAddRange(propertyMemberAst.Attributes);
                SafeAdd(propertyMemberAst.PropertyType);
                SafeAdd(propertyMemberAst.InitialValue);
                return null;
            }

            public object VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst)
            {
                SafeAddRange(typeDefinitionAst.Attributes);
                SafeAddRange(typeDefinitionAst.BaseTypes);
                SafeAddRange(typeDefinitionAst.Members);
                return null;
            }

            public object VisitUsingStatement(UsingStatementAst usingStatement)
            {
                SafeAdd(usingStatement.ModuleSpecification);
                SafeAdd(usingStatement.Name);
                SafeAdd(usingStatement.Alias);
                return null;
            }
#pragma warning restore SA1600
        }
    }
}
