using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Management.Automation.Language

function Expand-TypeImplementation {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [EditorCommand(DisplayName='Expand Closest Type to Implementation')]
    [CmdletBinding()]
    param(
        [Parameter(Position=0, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateNotNullOrEmpty()]
        [type[]]
        $Type
    )
    begin {
        $renderer = [TypeRenderer]::new()
        $group = @'
class(FullName, Name, DeclaredMethods) ::= <<
class New<Name> : <FullName> {
    <DeclaredMethods:methods(); separator={<\n><\n>}>
}

>>
methods(m) ::= <<
<m.ReturnType> <if(m.IsStatic)>static <endif><m.Name> (<m.Parameters:params(); separator=", ">) {
    throw [NotImplementedException]::new()
}
>>
params(p) ::= "<p.ParameterType> $<p.Name>"
'@
        $group    = New-StringTemplateGroup -Definition $group
        $instance = $group.GetType().GetProperty('Instance', 60).GetValue($group)

        $instance.RegisterRenderer([type], $renderer)
    }
    process {
        $typeList
        if ($Type) {
            $cursorPosition = $psEditor.GetEditorContext().CursorPosition
            # HACK: Temporary workaround until https://github.com/PowerShell/PowerShellEditorServices/pull/541
            #$targetExtent = $psEditor.GetEditorContext().CursorPosition | ConvertTo-ScriptExtent
            $targetExtent = [Microsoft.PowerShell.EditorServices.FullScriptExtent]::new(
                $psEditor.GetEditorContext().CurrentFile,
                [Microsoft.PowerShell.EditorServices.BufferRange]::new(
                    $cursorPosition.Line,
                    $cursorPosition.Column,
                    $cursorPosition.Line,
                    $cursorPosition.Column))
        } else {
            $ast = Find-Ast -AtCursor |
                Find-Ast -Family -First -IncludeStartingAst { $PSItem.TypeName }

            $targetExtent = $ast.Extent

            $resolvedType = $ast.TypeName.Name -as [type]

            if (-not $resolvedType) {
                # Type resolution scope is this function, so we need to pull the namespaces from
                # the current file and test against that.
                $using = Find-Ast { $_ -is [UsingStatementAst] -and
                                    $_.UsingStatementKind -eq 'Namespace' }

                foreach ($namespace in $using.Name) {
                    if ($resolvedType = ('{0}.{1}' -f $using.Name, $ast.TypeName.Name) -as [type]) {
                        break
                    }
                }
            }
            $Type = $resolvedType
        }
        $result = foreach ($aType in $Type) {
            Invoke-StringTemplate -Group $group -Name class -Parameters ($aType)
        }
        Set-ScriptExtent -Extent $targetExtent -Text $result
    }
}
