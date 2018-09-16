using System.Collections.Generic;
using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal class AssignmentVariableTargetVisitor : AstVisitor
    {
        private readonly List<VariableExpressionAst> _variables = new List<VariableExpressionAst>();

        private AssignmentVariableTargetVisitor()
        {
        }

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            _variables.Add(variableExpressionAst);
            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitIndexExpression(IndexExpressionAst indexExpressionAst) =>
            AstVisitAction.SkipChildren;

        public override AstVisitAction VisitMemberExpression(MemberExpressionAst memberExpressionAst) =>
            AstVisitAction.SkipChildren;

        public override AstVisitAction VisitInvokeMemberExpression(InvokeMemberExpressionAst methodCallAst) =>
            AstVisitAction.SkipChildren;

        internal static IList<VariableExpressionAst> GetTargets(AssignmentStatementAst assignment)
        {
            var visitor = new AssignmentVariableTargetVisitor();
            assignment.Left.Visit(visitor);
            return visitor._variables;
        }
    }
}
