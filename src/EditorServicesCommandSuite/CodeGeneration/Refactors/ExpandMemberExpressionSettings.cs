using System.Management.Automation;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class ExpandMemberExpressionSettings : RefactorConfiguration
    {
        [Parameter]
        [DefaultFromSetting("ExpandMemberExpression.AllowNonPublicMembers", Default = "$false")]
        public SwitchParameter AllowNonPublicMembers { get; set; }
    }
}
