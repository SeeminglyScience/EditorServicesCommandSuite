using namespace Microsoft.PowerShell.EditorServices
using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Management.Automation.Host
using namespace System.Management.Automation.Language

function Add-PinvokeMethod {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
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
            # Get the web service async so there isn't a hang before prompting for function name.
            $script:PinvokeWebService = async {
                $newWebServiceProxySplat = @{
                    Namespace = 'PinvokeWebService'
                    Class     = 'Main'
                    Uri       = 'http://pinvoke.net/pinvokeservice.asmx?wsdl'
                }
                New-WebServiceProxy @newWebServiceProxySplat
            }
        }

        # Return parameters if they exist, otherwise handle user input.
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

        # Some modules don't always return correctly, commonly structs.  This is a last ditch catch
        # all that parses the HTML content directly.
        # TODO: Replace calls to IE COM object with HtmlAgilityPack or similar.
        function GetUnsupportedSignature {
            $url = 'http://pinvoke.net/default.aspx/{0}/{1}.html' -f
                $functionInfo.Module,
                $functionInfo.Function
            try {
                $request = Invoke-WebRequest $url
            } catch {
                return
            }

            if ($request.Content -match 'The module <b>([^<]+)</b> does not exist') {
                $PSCmdlet.WriteDebug('Module {0} not found.' -f $matches[1])
                return
            }

            if ($request.Content -match 'You are about to create a new page called <b>([^<]+)</b>') {
                $PSCmdlet.WriteDebug('Function {0} not found' -f $matches[1])
                return
            }

            $nodes = $request.ParsedHtml.body.getElementsByClassName('TopicBody')[0].childNodes
            for ($i = 0; $i -lt $nodes.length; $i++) {

                $node = $nodes[$i]
                if ($node.tagName -ne 'H4') { continue }
                if ($node.innerText -notmatch 'C# Definition') { continue }

                $sig = $nodes[$i + 1]
                if ($sig.tagName -ne 'P' -or $sig.className -ne 'pre') { continue }
                return [PSCustomObject]@{
                    Signature = $sig.innerText -replace '\r?\n', '|'
                    Url = $url
                }
            }
        }

        # Get template and insertion extent.  If cursor is in a Add-Type command AST that has a member
        # definiton parameter, it will insert the signature into the existing command.  Otherwise it
        # will create a new Add-Type command expression at the current cursor position.
        function GetTemplateInfo {
            $defaultAction = {
                [PSCustomObject]@{
                    Template = "# Source: <SourceUri><\n>" +
                               "Add-Type -Namespace <Namespace> -Name <Class> -MemberDefinition '<\n><Signature>'"
                    Position = [FullScriptExtent]::new(
                        $context.CurrentFile,
                        [BufferRange]::new(
                            $context.CursorPosition.Line,
                            $context.CursorPosition.Column,
                            $context.CursorPosition.Line,
                            $context.CursorPosition.Column))
                }
            }
            $context = $psEditor.GetEditorContext()
            $commandAst = Find-Ast -AtCursor | Find-Ast -Ancestor -First { $PSItem -is [CommandAst] }

            if (-not $commandAst -or $commandAst.GetCommandName() -ne 'Add-Type') {
                return & $defaultAction
            }
            $binding = [StaticParameterBinder]::BindCommand($commandAst, $true)

            $memberDefinition = $binding.BoundParameters.MemberDefinition

            if (-not $memberDefinition) { return & $defaultAction }

            $targetOffset = $memberDefinition.Value.Extent.EndOffset - 1
            return [PSCustomObject]@{
                Template = '<\n><\n>// Source: <SourceUri><\n><Signature>'
                Position = [FullScriptExtent]::new($context.CurrentFile, $targetOffset, $targetOffset)
            }
        }

        # Get first non-whitespace character location if the line has text, otherwise get the current
        # cursor column.
        function GetIndentLevel {
            try {
                $context   = $psEditor.GetEditorContext()
                $lineStart = $context.CursorPosition.GetLineStart()
                $lineEnd   = $context.CursorPosition.GetLineEnd()
                $lineText  = $context.CurrentFile.GetText(
                    [BufferRange]::new(
                        $lineStart.Line,
                        $lineStart.Column,
                        $lineEnd.Line,
                        $lineEnd.Column))

                if ($lineText -match '\S') {
                    return $lineStart.Column - 1
                }
            } catch {
                $PSCmdlet.WriteDebug('Exception occurred while getting indent level')
            }
            return $context.CursorPosition.Column - 1
        }
    }
    end {
        $functionInfo = GetFunctionInfo $Function $Module
        if (-not $functionInfo) { return }

        $pinvoke = await $script:PinvokeWebService

        # Get signatures from pinvoke.net and filter by C#
        $signatureInfo = $null
        try {
            $signatureInfo = $pinvoke.
                GetResultsForFunction(
                    $functionInfo.Function,
                    $functionInfo.Module).
                Where{ $PSItem.Language -eq 'C#' }
        } catch [System.Web.Services.Protocols.SoapException] {
            if ($PSItem.Exception.Message -match 'but no signatures could be extracted') {
                $signatureInfo = GetUnsupportedSignature
            }
        }

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
        # - Escape single quotes
        $signature = $signatureInfo.Signature `
            -split '\|' `
            -join [Environment]::NewLine `
            -replace '(?<!public )(?:private )?(static|struct)', 'public $1' `
            -replace '\s+$' `
            -replace "'", "''"

        # Strip module name of numbers and make PascalCase.
        $formattedModuleName = [regex]::Replace(
            ($functionInfo.Module -replace '\d'),
            '^\w',
            { $args[0].Value.ToUpper() })

        $templateInfo = GetTemplateInfo
        $expression = Invoke-StringTemplate -Definition $templateInfo.Template -Parameters @{
            Namespace = 'PinvokeMethods'
            Class     = $formattedModuleName
            Signature = $signature
            SourceUri = $signatureInfo.Url.Where({ $PSItem }, 'First')[0]
        }

        $indentLevel = GetIndentLevel
        $indent      = ' ' * ($indentLevel - 1)
        $expression  = $expression -split '\r?\n' -join ([Environment]::NewLine + $indent)

        Set-ScriptExtent -Extent $templateInfo.Position -Text $expression
    }
}
