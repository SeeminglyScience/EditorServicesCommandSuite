using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Inference;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Language
{
    internal sealed class SelectionVariableAnalysisVisitor : ExtentRestrictedAnalysisBase
    {
        private static readonly HashSet<string> s_commonVariableNameParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ov", "OutVariable",
            "pv", "PipelineVariable",
            "ev", "ErrorVariable",
            "wv", "WarningVariable",
            "iv", "InformationVariable",
        };

        private static readonly PSTypeName s_objectPSType = new PSTypeName(typeof(object));

        private readonly Dictionary<string, IScriptExtent> _assignedVariables = new Dictionary<string, IScriptExtent>(StringComparer.OrdinalIgnoreCase);

        private readonly List<VariableExpressionAst> _variableExpressions = new List<VariableExpressionAst>();

        private SelectionVariableAnalysisVisitor(IScriptExtent selectedExtent)
            : base(selectedExtent)
        {
        }

        public override AstVisitAction VisitAssignmentStatement(AssignmentStatementAst assignmentStatementAst)
        {
            AstVisitAction parentAction = base.VisitAssignmentStatement(assignmentStatementAst);
            if (parentAction != AstVisitAction.Continue)
            {
                return parentAction;
            }

            foreach (VariableExpressionAst variable in AssignmentVariableTargetVisitor.GetTargets(assignmentStatementAst))
            {
                RegisterAssignedVariable(variable);
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitCommand(CommandAst commandAst)
        {
            if (!_selectedExtent.ContainsExtent(commandAst.Extent))
            {
                return AstVisitAction.Continue;
            }

            // Look for CommonParameters that create variables (e.g. OutVariable)
            for (var i = 0; i < commandAst.CommandElements.Count; i++)
            {
                if (!(commandAst.CommandElements[i] is CommandParameterAst commandParameter))
                {
                    continue;
                }

                if (!s_commonVariableNameParameters.Contains(commandParameter.ParameterName))
                {
                    continue;
                }

                CommandElementAst argument = commandParameter.Argument;
                if (argument == null)
                {
                    if (i == commandAst.CommandElements.Count - 1)
                    {
                        return AstVisitAction.Continue;
                    }

                    i++;
                    argument = commandAst.CommandElements[i];
                }

                if (!(argument is StringConstantExpressionAst constant))
                {
                    continue;
                }

                // If the variable name doesn't start with '+' then add it as is.
                if (constant.Value.Length < 2 || constant.Value[0].Equals(Symbols.Plus))
                {
                    RegisterAssignedVariable(constant.Value, constant.Extent);
                    continue;
                }

                // If it does start with '+' then remove it from the name and adjust the
                // script extent we save.
                RegisterAssignedVariable(
                    constant.Value.Substring(1),
                    PositionUtilities.NewScriptExtent(
                        constant.Extent,
                        constant.Extent.StartOffset + 1,
                        constant.Extent.EndOffset));
            }

            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitForEachStatement(ForEachStatementAst forEachStatementAst)
        {
            if (!_selectedExtent.ContainsExtent(forEachStatementAst.Extent))
            {
                return AstVisitAction.Continue;
            }

            RegisterAssignedVariable(forEachStatementAst.Variable);
            return AstVisitAction.Continue;
        }

        public override AstVisitAction VisitVariableExpression(VariableExpressionAst variableExpressionAst)
        {
            if (!_selectedExtent.ContainsExtent(variableExpressionAst.Extent))
            {
                return AstVisitAction.Continue;
            }

            _variableExpressions.Add(variableExpressionAst);
            return AstVisitAction.Continue;
        }

        internal static async Task<Dictionary<string, SelectionVariableAnalysisResult>> ProcessSelection(
            ScriptBlockAst rootAst,
            IScriptExtent selection,
            ThreadController pipelineThread,
            CancellationToken cancellationToken = default)
        {
            var visitor = new SelectionVariableAnalysisVisitor(selection);
            rootAst.Visit(visitor);
            var parameters = new Dictionary<string, PSTypeName>();
            var externalVariables = new List<VariableExpressionAst>();
            var parameterDetails =
                new Dictionary<string, SelectionVariableAnalysisResult>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (VariableExpressionAst variable in visitor._variableExpressions)
            {
                if (!variable.VariablePath.IsUnqualified)
                {
                    continue;
                }

                bool doesAssignmentExist = visitor._assignedVariables.TryGetValue(
                    variable.VariablePath.UserPath,
                    out IScriptExtent assignmentExtent);

                if (doesAssignmentExist && !variable.Extent.IsBefore(assignmentExtent))
                {
                    continue;
                }

                if (parameterDetails.TryGetValue(
                    variable.VariablePath.UserPath,
                    out SelectionVariableAnalysisResult details))
                {
                    details.Occurrences.Add(variable);

                    // Check if the stored type name is specific enough. If type name couldn't be
                    // resolved then it's probably accurate, just not loaded.
                    if (details.InferredType.Type != typeof(object))
                    {
                        continue;
                    }

                    details.InferredType = await variable.GetInferredTypeAsync(
                        pipelineThread,
                        cancellationToken,
                        defaultValue: s_objectPSType)
                        .ConfigureAwait(false);
                    continue;
                }

                parameterDetails.Add(
                    variable.VariablePath.UserPath,
                    new SelectionVariableAnalysisResult(
                        variable,
                        await variable.GetInferredTypeAsync(
                            pipelineThread,
                            cancellationToken,
                            defaultValue: s_objectPSType)
                            .ConfigureAwait(false)));
            }

            return parameterDetails;
        }

        private void RegisterAssignedVariable(VariableExpressionAst variable)
        {
            if (!variable.VariablePath.IsUnqualified)
            {
                return;
            }

            RegisterAssignedVariable(variable.VariablePath.UserPath, variable.Extent);
        }

        private void RegisterAssignedVariable(string variableName, IScriptExtent extent)
        {
            if (_assignedVariables.ContainsKey(variableName))
            {
                return;
            }

            _assignedVariables.Add(variableName, extent);
        }
    }
}
