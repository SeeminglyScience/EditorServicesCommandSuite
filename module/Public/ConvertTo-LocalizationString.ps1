using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Management.Automation.Language

function ConvertTo-LocalizationString {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [CmdletBinding()]
    [EditorCommand(DisplayName='Add Closest String to Localization File')]
    param(
        [System.Management.Automation.Language.Ast]
        $Ast = (Find-Ast -AtCursor),

        [string]
        $Name
    )
    end {
        $Ast = GetAncestorOrThrow $Ast -AstTypeName StringConstantExpressionAst -ErrorContext $PSCmdlet

        if (-not $Name) {
            if ($Host -is [System.Management.Automation.Host.IHostSupportsInteractiveSession]) {
                $Name = ReadInputPrompt $Strings.StringNamePrompt
            } else {
                $Name = (Split-Path $psEditor.GetEditorContext().CurrentFile.Path -Leaf) +
                         '-' +
                         [guid]::NewGuid().Guid
            }
        }
        if (-not $Name) {
            ThrowError -Exception ([ArgumentException]::new($Strings.StringNamePromptFail)) `
                       -Id        StringNamePromptFail `
                       -Category  InvalidArgument `
                       -Target    $Name
        }

        $originalContents = $Ast.Value
        $Ast | Set-ScriptExtent -Text ('$Strings.{0}' -f $Name)

        try {
            SetEditorLocation (ResolveRelativePath (GetSettings).StringLocalizationManifest)
        } catch [System.Management.Automation.ItemNotFoundException] {
            $exception = [System.Management.Automation.PSArgumentException]::new(
                $Strings.InvalidSettingValue -f 'StringLocalizationManifest')
            ThrowError -Exception  $exception `
                       -Id         InvalidSettingValue`
                       -Category   ObjectNotFound`
                       -Target     $null `
                       -Show
            return
        }

        $hereString = Find-Ast { 'SingleQuotedHereString' -eq $_.StringConstantType } -First

        $newHereString = $hereString.Extent.Text -replace
            "(\r?\n)'@",
            ('$1' + $Name + '=' + $originalContents + '$1''@')

        $hereString | Set-ScriptExtent -Text $newHereString
    }
}
