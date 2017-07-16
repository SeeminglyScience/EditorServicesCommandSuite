using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Collections.Generic
using namespace System.Management.Automation.Language

function Set-HangingIndent {
    [EditorCommand(DisplayName='Set Selection Indent to Selection Start')]
    [CmdletBinding()]
    param()
    end {
        $context   = $psEditor.GetEditorContext()
        $selection = $context.SelectedRange | ConvertTo-ScriptExtent

        foreach ($token in ($selection | Get-Token)) {
            if ('NewLine', 'LineContinuation' -notcontains $token.Kind) {
                continue
            }
            if (-not $foreach.MoveNext()) { break }

            $current = $foreach.Current

            $difference = $selection.StartColumnNumber - $current.Extent.StartColumnNumber
            if ($difference -gt 0) {

                # HACK: Temporary workaround until https://github.com/PowerShell/PowerShellEditorServices/pull/541
                #ConvertTo-ScriptExtent -Line $current.Extent.StartLineNumber |
                $targetExtent = [Microsoft.PowerShell.EditorServices.FullScriptExtent]::new(
                    $psEditor.GetEditorContext().CurrentFile,
                    [Microsoft.PowerShell.EditorServices.BufferRange]::new(
                        $current.Extent.StartLineNumber,
                        1,
                        $current.Extent.StartLineNumber,
                        1))

                $targetExtent | Set-ScriptExtent -Text (' ' * $difference)
            }
        }
    }
}
