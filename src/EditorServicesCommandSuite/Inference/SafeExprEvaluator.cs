// This file is based on a file from https://github.com/PowerShell/PowerShell, and
// edited to use public API's where possible. While not generated, the comment below
// is included to exclude it from StyleCop analysis.
// <auto-generated/>

using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Inference
{
    /// <summary>
    /// This class is very similar to the restricted langauge checker, but it is meant to allow more things, yet still
    /// be considered "safe", at least in the sense that tab completion can rely on it to not do bad things.  The primary
    /// use is for intellisense where you don't want to run arbitrary code, but you do want to know the values
    /// of various expressions so you can get the members.
    /// </summary>
    internal class SafeExprEvaluator : ICustomAstVisitor2
    {
        internal static ScriptBlock GetExpressionScriptBlock(ExpressionAst ast)
        {
            return
                new ScriptBlockAst(
                    extent: Empty.Extent.Untitled,
                    paramBlock: new ParamBlockAst(
                        Empty.Extent.Untitled,
                        Array.Empty<AttributeAst>(),
                        Array.Empty<ParameterAst>()),
                    statements: new StatementBlockAst(
                        Empty.Extent.Get(),
                        new StatementAst[1]
                        {
                            new CommandExpressionAst(
                                Empty.Extent.Untitled,
                                (ExpressionAst)ast.Copy(),
                                Array.Empty<RedirectionAst>()),
                        },
                        Array.Empty<TrapStatementAst>()),
                    isFilter: false)
                .GetScriptBlock();
        }

        internal static bool TrySafeEval(ExpressionAst ast, ThreadController pipelineThread, out object value)
        {
            if (!(bool)ast.Visit(new SafeExprEvaluator()))
            {
                value = null;
                return false;
            }

            EngineIntrinsics engine;
            PSCmdlet cmdlet;
            (engine, cmdlet) = pipelineThread.GetThreadContext();
            ScriptBlock scriptBlock = GetExpressionScriptBlock(ast);
            scriptBlock.SetSessionStateInternal(cmdlet.SessionState);
            try
            {
                value = scriptBlock.InvokeReturnAsIs();
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        public object VisitErrorStatement(ErrorStatementAst errorStatementAst) { return false; }
        public object VisitErrorExpression(ErrorExpressionAst errorExpressionAst) { return false; }
        public object VisitScriptBlock(ScriptBlockAst scriptBlockAst) { return false; }
        public object VisitParamBlock(ParamBlockAst paramBlockAst) { return false; }
        public object VisitNamedBlock(NamedBlockAst namedBlockAst) { return false; }
        public object VisitTypeConstraint(TypeConstraintAst typeConstraintAst) { return false; }
        public object VisitAttribute(AttributeAst attributeAst) { return false; }
        public object VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) { return false; }
        public object VisitParameter(ParameterAst parameterAst) { return false; }
        public object VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) { return false; }
        public object VisitIfStatement(IfStatementAst ifStmtAst) { return false; }
        public object VisitTrap(TrapStatementAst trapStatementAst) { return false; }
        public object VisitSwitchStatement(SwitchStatementAst switchStatementAst) { return false; }
        public object VisitDataStatement(DataStatementAst dataStatementAst) { return false; }
        public object VisitForEachStatement(ForEachStatementAst forEachStatementAst) { return false; }
        public object VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) { return false; }
        public object VisitForStatement(ForStatementAst forStatementAst) { return false; }
        public object VisitWhileStatement(WhileStatementAst whileStatementAst) { return false; }
        public object VisitCatchClause(CatchClauseAst catchClauseAst) { return false; }
        public object VisitTryStatement(TryStatementAst tryStatementAst) { return false; }
        public object VisitBreakStatement(BreakStatementAst breakStatementAst) { return false; }
        public object VisitContinueStatement(ContinueStatementAst continueStatementAst) { return false; }
        public object VisitReturnStatement(ReturnStatementAst returnStatementAst) { return false; }
        public object VisitExitStatement(ExitStatementAst exitStatementAst) { return false; }
        public object VisitThrowStatement(ThrowStatementAst throwStatementAst) { return false; }
        public object VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) { return false; }
        public object VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) { return false; }
        // REVIEW: we could relax this to allow specific commands
        public object VisitCommand(CommandAst commandAst) { return false; }
        public object VisitCommandExpression(CommandExpressionAst commandExpressionAst) { return false; }
        public object VisitCommandParameter(CommandParameterAst commandParameterAst) { return false; }
        public object VisitFileRedirection(FileRedirectionAst fileRedirectionAst) { return false; }
        public object VisitMergingRedirection(MergingRedirectionAst mergingRedirectionAst) { return false; }
        public object VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst) { return false; }
        public object VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) { return false; }
        public object VisitBlockStatement(BlockStatementAst blockStatementAst) { return false; }
        public object VisitInvokeMemberExpression(InvokeMemberExpressionAst invokeMemberExpressionAst) { return false; }
        public object VisitUsingExpression(UsingExpressionAst usingExpressionAst) { return false; }
        public object VisitTypeDefinition(TypeDefinitionAst typeDefinitionAst) { return false; }
        public object VisitPropertyMember(PropertyMemberAst propertyMemberAst) { return false; }
        public object VisitFunctionMember(FunctionMemberAst functionMemberAst) { return false; }
        public object VisitBaseCtorInvokeMemberExpression(BaseCtorInvokeMemberExpressionAst baseCtorInvokeMemberExpressionAst) { return false; }
        public object VisitUsingStatement(UsingStatementAst usingStatementAst) { return false; }
        public object VisitConfigurationDefinition(ConfigurationDefinitionAst configurationDefinitionAst)
        {
            return configurationDefinitionAst.Body.Visit(this);
        }
        public object VisitDynamicKeywordStatement(DynamicKeywordStatementAst dynamicKeywordStatementAst)
        {
            return false;
        }

        public object VisitStatementBlock(StatementBlockAst statementBlockAst)
        {
            if (statementBlockAst.Traps != null) return false;
            // REVIEW: we could relax this to allow multiple statements
            if (statementBlockAst.Statements.Count > 1) return false;
            var pipeline = statementBlockAst.Statements.FirstOrDefault();
            return pipeline != null && (bool)pipeline.Visit(this);
        }

        public object VisitPipeline(PipelineAst pipelineAst)
        {
            var expr = pipelineAst.GetPureExpression();
            return expr != null && (bool)expr.Visit(this);
        }

        public object VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst)
        {
            return (bool)binaryExpressionAst.Left.Visit(this) && (bool)binaryExpressionAst.Right.Visit(this);
        }

        public object VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst)
        {
            return (bool)unaryExpressionAst.Child.Visit(this);
        }

        public object VisitConvertExpression(ConvertExpressionAst convertExpressionAst)
        {
            return (bool)convertExpressionAst.Child.Visit(this);
        }

        public object VisitConstantExpression(ConstantExpressionAst constantExpressionAst)
        {
            return true;
        }

        public object VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
        {
            return true;
        }

        public object VisitSubExpression(SubExpressionAst subExpressionAst)
        {
            return subExpressionAst.SubExpression.Visit(this);
        }

        public object VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            return true;
        }

        public object VisitTypeExpression(TypeExpressionAst typeExpressionAst)
        {
            return true;
        }

        public object VisitMemberExpression(MemberExpressionAst memberExpressionAst)
        {
            return (bool)memberExpressionAst.Expression.Visit(this) && (bool)memberExpressionAst.Member.Visit(this);
        }

        public object VisitIndexExpression(IndexExpressionAst indexExpressionAst)
        {
            return (bool)indexExpressionAst.Target.Visit(this) && (bool)indexExpressionAst.Index.Visit(this);
        }

        public object VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
        {
            return arrayExpressionAst.SubExpression.Visit(this);
        }

        public object VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
        {
            return arrayLiteralAst.Elements.All(e => (bool)e.Visit(this));
        }

        public object VisitHashtable(HashtableAst hashtableAst)
        {
            foreach (var keyValuePair in hashtableAst.KeyValuePairs)
            {
                if (!(bool)keyValuePair.Item1.Visit(this))
                    return false;
                if (!(bool)keyValuePair.Item2.Visit(this))
                    return false;
            }
            return true;
        }

        public object VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst)
        {
            return true;
        }

        public object VisitParenExpression(ParenExpressionAst parenExpressionAst)
        {
            return parenExpressionAst.Pipeline.Visit(this);
        }
    }
}
