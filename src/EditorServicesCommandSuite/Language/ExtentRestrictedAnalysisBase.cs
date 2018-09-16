using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal abstract class ExtentRestrictedAnalysisBase : AstVisitor2
    {
        protected readonly IScriptExtent _selectedExtent;

        protected ExtentRestrictedAnalysisBase(IScriptExtent selectedExtent)
        {
            _selectedExtent = selectedExtent;
        }

        public override AstVisitAction VisitArrayExpression(ArrayExpressionAst arrayExpressionAst) => DoesOverlapExtent(arrayExpressionAst);

        public override AstVisitAction VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst) => DoesOverlapExtent(arrayLiteralAst);

        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst) => DoesOverlapExtent(assignmentStatementAst);

        public override AstVisitAction VisitAttribute(AttributeAst attributeAst) => DoesOverlapExtent(attributeAst);

        public override AstVisitAction VisitAttributedExpression(AttributedExpressionAst attributedExpressionAst) => DoesOverlapExtent(attributedExpressionAst);

        public override AstVisitAction VisitBinaryExpression(BinaryExpressionAst binaryExpressionAst) => DoesOverlapExtent(binaryExpressionAst);

        public override AstVisitAction VisitBlockStatement(BlockStatementAst blockStatementAst) => DoesOverlapExtent(blockStatementAst);

        public override AstVisitAction VisitBreakStatement(BreakStatementAst breakStatementAst) => DoesOverlapExtent(breakStatementAst);

        public override AstVisitAction VisitCatchClause(CatchClauseAst catchClauseAst) => DoesOverlapExtent(catchClauseAst);

        public override AstVisitAction VisitCommand(CommandAst commandAst) => DoesOverlapExtent(commandAst);

        public override AstVisitAction VisitCommandExpression(CommandExpressionAst commandExpressionAst) => DoesOverlapExtent(commandExpressionAst);

        public override AstVisitAction VisitCommandParameter(CommandParameterAst commandParameterAst) => DoesOverlapExtent(commandParameterAst);

        public override AstVisitAction VisitConstantExpression(ConstantExpressionAst constantExpressionAst) => DoesOverlapExtent(constantExpressionAst);

        public override AstVisitAction VisitContinueStatement(ContinueStatementAst continueStatementAst) => DoesOverlapExtent(continueStatementAst);

        public override AstVisitAction VisitConvertExpression(ConvertExpressionAst convertExpressionAst) => DoesOverlapExtent(convertExpressionAst);

        public override AstVisitAction VisitDataStatement(DataStatementAst dataStatementAst) => DoesOverlapExtent(dataStatementAst);

        public override AstVisitAction VisitDoUntilStatement(DoUntilStatementAst doUntilStatementAst) => DoesOverlapExtent(doUntilStatementAst);

        public override AstVisitAction VisitDoWhileStatement(DoWhileStatementAst doWhileStatementAst) => DoesOverlapExtent(doWhileStatementAst);

        public override AstVisitAction VisitErrorExpression(ErrorExpressionAst errorExpressionAst) => DoesOverlapExtent(errorExpressionAst);

        public override AstVisitAction VisitErrorStatement(ErrorStatementAst errorStatementAst) => DoesOverlapExtent(errorStatementAst);

        public override AstVisitAction VisitExitStatement(ExitStatementAst exitStatementAst) => DoesOverlapExtent(exitStatementAst);

        public override AstVisitAction VisitExpandableStringExpression(ExpandableStringExpressionAst expandableStringExpressionAst) => DoesOverlapExtent(expandableStringExpressionAst);

        public override AstVisitAction VisitFileRedirection(FileRedirectionAst redirectionAst) => DoesOverlapExtent(redirectionAst);

        public override AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst) => DoesOverlapExtent(forEachStatementAst);

        public override AstVisitAction VisitForStatement(ForStatementAst forStatementAst) => DoesOverlapExtent(forStatementAst);

        public override AstVisitAction VisitFunctionDefinition(FunctionDefinitionAst functionDefinitionAst) => DoesOverlapExtent(functionDefinitionAst);

        public override AstVisitAction VisitHashtable(HashtableAst hashtableAst) => DoesOverlapExtent(hashtableAst);

        public override AstVisitAction VisitIfStatement(IfStatementAst ifStmtAst) => DoesOverlapExtent(ifStmtAst);

        public override AstVisitAction VisitIndexExpression(IndexExpressionAst indexExpressionAst) => DoesOverlapExtent(indexExpressionAst);

        public override AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst methodCallAst) => DoesOverlapExtent(methodCallAst);

        public override AstVisitAction VisitMemberExpression(MemberExpressionAst memberExpressionAst) => DoesOverlapExtent(memberExpressionAst);

        public override AstVisitAction VisitMergingRedirection(MergingRedirectionAst redirectionAst) => DoesOverlapExtent(redirectionAst);

        public override AstVisitAction VisitNamedAttributeArgument(NamedAttributeArgumentAst namedAttributeArgumentAst) => DoesOverlapExtent(namedAttributeArgumentAst);

        public override AstVisitAction VisitNamedBlock(NamedBlockAst namedBlockAst) => DoesOverlapExtent(namedBlockAst);

        public override AstVisitAction VisitParamBlock(ParamBlockAst paramBlockAst) => DoesOverlapExtent(paramBlockAst);

        public override AstVisitAction VisitParameter(ParameterAst parameterAst) => DoesOverlapExtent(parameterAst);

        public override AstVisitAction VisitParenExpression(ParenExpressionAst parenExpressionAst) => DoesOverlapExtent(parenExpressionAst);

        public override AstVisitAction VisitPipeline(PipelineAst pipelineAst) => DoesOverlapExtent(pipelineAst);

        public override AstVisitAction VisitReturnStatement(ReturnStatementAst returnStatementAst) => DoesOverlapExtent(returnStatementAst);

        public override AstVisitAction VisitScriptBlock(ScriptBlockAst scriptBlockAst) => DoesOverlapExtent(scriptBlockAst);

        public override AstVisitAction VisitScriptBlockExpression(ScriptBlockExpressionAst scriptBlockExpressionAst) => DoesOverlapExtent(scriptBlockExpressionAst);

        public override AstVisitAction VisitStatementBlock(StatementBlockAst statementBlockAst) => DoesOverlapExtent(statementBlockAst);

        public override AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst) => DoesOverlapExtent(stringConstantExpressionAst);

        public override AstVisitAction VisitSubExpression(SubExpressionAst subExpressionAst) => DoesOverlapExtent(subExpressionAst);

        public override AstVisitAction VisitSwitchStatement(SwitchStatementAst switchStatementAst) => DoesOverlapExtent(switchStatementAst);

        public override AstVisitAction VisitThrowStatement(ThrowStatementAst throwStatementAst) => DoesOverlapExtent(throwStatementAst);

        public override AstVisitAction VisitTrap(TrapStatementAst trapStatementAst) => DoesOverlapExtent(trapStatementAst);

        public override AstVisitAction VisitTryStatement(TryStatementAst tryStatementAst) => DoesOverlapExtent(tryStatementAst);

        public override AstVisitAction VisitTypeConstraint(TypeConstraintAst typeConstraintAst) => DoesOverlapExtent(typeConstraintAst);

        public override AstVisitAction VisitTypeExpression(TypeExpressionAst typeExpressionAst) => DoesOverlapExtent(typeExpressionAst);

        public override AstVisitAction VisitUnaryExpression(UnaryExpressionAst unaryExpressionAst) => DoesOverlapExtent(unaryExpressionAst);

        public override AstVisitAction VisitUsingExpression(UsingExpressionAst usingExpressionAst) => DoesOverlapExtent(usingExpressionAst);

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst) => DoesOverlapExtent(variableExpressionAst);

        public override AstVisitAction VisitWhileStatement(WhileStatementAst whileStatementAst) => DoesOverlapExtent(whileStatementAst);

        private AstVisitAction DoesOverlapExtent(Ast ast)
        {
            if (ast.Extent.IsBefore(_selectedExtent))
            {
                return AstVisitAction.SkipChildren;
            }

            if (ast.Extent.IsAfter(_selectedExtent))
            {
                return AstVisitAction.StopVisit;
            }

            return AstVisitAction.Continue;
        }
    }
}
