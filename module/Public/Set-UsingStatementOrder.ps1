using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Management.Automation.Language

function Set-UsingStatementOrder {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [CmdletBinding()]
    [EditorCommand(DisplayName='Sort Using Statements')]
    param()
    end {
        $statements = Find-Ast { $PSItem -is [UsingStatementAst] }

        $groups = $statements | Group-Object UsingStatementKind -AsHashTable -AsString

        $sorted = & {
            if ($groups.Assembly)  { $groups.Assembly  | Sort-Object Name }
            if ($groups.Module)    { $groups.Module    | Sort-Object Name }
            if ($groups.Namespace) { $groups.Namespace | Sort-Object Name }
        } | ForEach-Object -MemberName ToString

        $statements | Join-ScriptExtent | Set-ScriptExtent -Text ($sorted -join [Environment]::NewLine)
    }
}
