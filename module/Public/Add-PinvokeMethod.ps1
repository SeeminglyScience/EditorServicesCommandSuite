using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Management.Automation.Host

function Add-PinvokeMethod {
    [EditorCommand(DisplayName='Insert Pinvoke Method Definition')]
    [CmdletBinding()]
    param(
        [ValidateNotNullOrEmpty()]
        [string]
        $Function,

        [ValidateNotNullOrEmpty()]
        [string]
        $Module
    )
    begin {
        if (-not $script:PinvokeWebService) {
            $script:PinvokeWebService = async {
                $newWebServiceProxySplat = @{
                    Namespace = 'PinvokeWebService'
                    Class     = 'Main'
                    Uri       = 'http://pinvoke.net/pinvokeservice.asmx?wsdl'
                }
                New-WebServiceProxy @newWebServiceProxySplat
            }
        }
        function GetFunctionInfo([string] $functionName, [string] $moduleName) {
            if ($functionName -and $moduleName) {
                return [PSCustomObject]@{
                    Function = $functionName
                    Module   = $moduleName
                }
            }
            if (-not $functionName) {
                $functionName = ReadInputPrompt $Strings.PInvokeFunctionNamePrompt
                if (-not $functionName) { return }
            }
            $pinvoke = await $script:PinvokeWebService
            $searchResults = $pinvoke.SearchFunction($functionName, $null)

            if (-not $searchResults) {
                ThrowError -Exception ([ArgumentException]::new($Strings.CannotFindPInvokeFunction -f $functionName)) `
                           -Id        CannotFindPInvokeFunction `
                           -Category  InvalidArgument `
                           -Target    $functionName `
                           -Show
            }

            $choice = $null
            if ($searchResults.Count -gt 1) {
                $choices = $searchResults.ForEach{
                    [ChoiceDescription]::new(
                        $PSItem.Function,
                        ('Module: {0} Function: {1}' -f $PSItem.Module, $PSItem.Function))
                }
                $choice = ReadChoicePrompt $Strings.PInvokeFunctionChoice -Choices $choices
                if ($null -eq $choice) { return }
            }
            $searchResults[[int]$choice]
        }
    }
    end {
        $functionInfo = GetFunctionInfo $Function $Module
        if (-not $functionInfo) { return }

        $pinvoke = await $script:PinvokeWebService

        # Get signatures from pinvoke.net and filter by C#
        $signatureInfo = $pinvoke.
            GetResultsForFunction(
                $functionInfo.Function,
                $functionInfo.Module).
            Where{ $PSItem.Language -eq 'C#' }

        if (-not $signatureInfo) {
            ThrowError -Exception ([InvalidOperationException]::new($Strings.MissingPInvokeSignature)) `
                       -Id        MissingPInvokeSignature `
                       -Category  InvalidOperation `
                       -Target    $functionInfo `
                       -Show
        }

        # - Replace pipes with new lines
        # - Add public modifier
        # - Trim white trailing whitespace
        $signature = $signatureInfo.Signature `
            -split '\|' `
            -join [Environment]::NewLine `
            -replace '(?<!public )static', 'public static' `
            -replace '\s+$'

        $formattedModuleName = [regex]::Replace(
            ($functionInfo.Module -replace '\d'),
            '^\w',
            { $args[0].Value.ToUpper() })

        $template   = "# Source: <SourceUri><\n>" +
                      "Add-Type -Namespace <Namespace> -Name <Class> -MemberDefinition '<\n><Signature>'"
        $expression = Invoke-StringTemplate -Definition $template -Parameters @{
            Namespace = 'PinvokeMethods'
            Class     = $formattedModuleName
            Signature = $signature
            SourceUri = $signatureInfo.Url.Where({ $PSItem }, 'First')[0]
        }

        $indent     = ' ' * ($psEditor.GetEditorContext().CursorPosition.Column - 1)
        $expression = $expression -split '\r?\n' -join ([Environment]::NewLine + $indent)

        $psEditor.GetEditorContext().CurrentFile.InsertText($expression)
    }
}
