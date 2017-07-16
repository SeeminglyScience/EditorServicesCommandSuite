using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Management.Automation
using namespace System.Management.Automation.Language

function Expand-Expression {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [EditorCommand(DisplayName='Expand Selection Text to Output')]
    [CmdletBinding()]
    param(
        [Parameter(ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateNotNullOrEmpty()]
        [Alias('Extent')]
        [System.Management.Automation.Language.IScriptExtent[]]
        $InputObject = ($psEditor.GetEditorContext().SelectedRange | ConvertTo-ScriptExtent)
    )
    process {
        foreach ($object in $InputObject) {
            if ([string]::IsNullOrWhiteSpace($object.Text)) {
                $message = $Strings.ExpandEmptyExtent -f $object.StartOffset, $object.File
                ThrowError -Exception ([InvalidOperationException]::new($message)) `
                           -Id        ExpandEmptyExtent `
                           -Category  InvalidOperation `
                           -Target    $object `
                           -Show
            }
            $parseErrors = $null
            $null = [Parser]::ParseInput(
                <# input:  #> $object.Text,
                <# tokens: #> [ref]$null,
                <# errors: #> [ref]$parseErrors
            )
            if ($parseErrors) {
                ThrowError -Exception ([ParseException]::new($parseErrors)) `
                           -Id        ExpandExpressionParseError `
                           -Category  InvalidArgument `
                           -Target    $object `
                           -Show
            }
            try {
                $output = & ([scriptblock]::Create($object.Text)) | Out-String
            } catch {
                ThrowError -ErrorRecord $PSItem -Show
            }

            Set-ScriptExtent -Extent $object -Text $output
        }
    }
}
