using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;

namespace EditorServicesCommandSuite.Language
{
    internal class SelectionVariableAnalysisResult
    {
        public SelectionVariableAnalysisResult(
            VariableExpressionAst firstOccurrence,
            PSTypeName inferredType)
        {
            VariableName = firstOccurrence.VariablePath.UserPath;
            InferredType = inferredType;
            Occurrences.Add(firstOccurrence);
        }

        public string VariableName { get; }

        public List<VariableExpressionAst> Occurrences { get; } = new List<VariableExpressionAst>();

        public PSTypeName InferredType { get; set; }
    }
}
