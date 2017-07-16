using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Collections.Generic
using namespace System.Linq
using namespace System.Management.Automation.Language

function Remove-Semicolon {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [CmdletBinding()]
    [EditorCommand(DisplayName='Remove cosmetic semicolons')]
    param()
    end {
        $propertyDefinitions = Find-Ast { $PSItem -is [PropertyMemberAst] }
        $tokens = (Get-Token).Where{ $PSItem.Extent.StartOffset + 1 -notin $propertyDefinitions.Extent.EndOffset }

        $extentsToRemove = [List[IScriptExtent]]::new()

        for ($i = 0; $i -lt $tokens.Count; $i++) {
            if ($tokens[$i].Kind -ne [TokenKind]::Semi) { continue }

            if ($tokens[$i+1].Kind -eq [TokenKind]::NewLine) {
                $extentsToRemove.Add($tokens[$i].Extent)
            }
        }
        [Enumerable]::Distinct($extentsToRemove) | Set-ScriptExtent -Text ''
    }
}
