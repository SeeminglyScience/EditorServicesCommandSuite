using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Management.Automation.Language

function ConvertTo-MarkdownHelp {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [EditorCommand(DisplayName='Generate Markdown from Closest Function')]
    [CmdletBinding()]
    param(
        [System.Management.Automation.Language.Ast]
        $Ast
    )
    end {
        $Ast = GetAncestorOrThrow $Ast -AstTypeName FunctionDefinitionAst -ErrorContext $PSCmdlet

        $settings = GetSettings
        $manifest = GetInferredManifest
        $docsPath = Join-Path (ResolveRelativePath $settings.MarkdownDocsPath) $PSCulture
        # If project uri is defined in the manifest then take a guess at what the online uri
        # should be.
        if ($projectUri = $manifest.PrivateData.PSData.ProjectUri) {
            $normalizedDocs = $PSCmdlet.SessionState.Path.NormalizeRelativePath(
                $docsPath,
                $psEditor.Workspace.Path)

            $onlineUri = $projectUri, $normalizedDocs, ($Ast.Name + '.md') -join '/' -replace '\\', '/'
        }

        # Wrap this whole thing in a try/finally so we can dispose of temp files and PowerShell
        # session in event of an error or CTRL + C
        try {
            $tempFolder = Join-Path $env:TEMP -ChildPath (New-Guid).Guid
            $null = New-Item $tempFolder -ItemType Directory

            # Load the the module and create markdown in a new runspace so we don't pollute the
            # current session.
            $ps = [powershell]::Create('NewRunspace')
            $null = $ps.AddScript('
                param($manifestPath, $commandName, $onlineUrl, $tempFolder)

                Import-Module $manifestPath
                New-MarkdownHelp -Command $commandName `
                                 -OnlineVersionUrl $onlineUrl `
                                 -OutputFolder $tempFolder
            ').
                AddArgument((ResolveRelativePath $settings.SourceManifestPath)).
                AddArgument($Ast.Name).
                AddArgument($onlineUri).
                AddArgument($tempFolder).
                Invoke()

            $markdownFile    = Get-ChildItem $tempFolder\*.md | Select-Object -First 1
            $markdownContent = Get-Content $markdownFile.FullName -Raw

        } finally {
            if ($ps) { $ps.Dispose() }

            if ($tempFolder -and (Test-Path $tempFolder) -and $tempFolder -match 'Temp\\^[A-z0-9-]+$') {
                Remove-Item $tempFolder -Recurse -Force
            }
        }

        if ([string]::IsNullOrWhiteSpace($markdownContent)) {
            ThrowError -Exception ([InvalidOperationException]::new($Strings.FailureGettingMarkdown)) `
                       -Id        FailureGettingMarkdown `
                       -Category  InvalidOperation `
                       -Target    $markdownContent `
                       -Show
        }

        $helpToken = $ast | Get-Token |
            Where-Object Kind -EQ Comment |
            Where-Object Text -Match '\.EXTERNALHELP|\.SYNOPSIS'

        $helpIndentLevel = $helpToken.Extent.StartColumnNumber - 1

        $newHelpComment = '<#',
                          ('.EXTERNALHELP {0}-help.xml' -f $manifest.Name),
                          '#>' -join ([Environment]::NewLine + (' ' * $helpIndentLevel))

        if ($helpToken.Text -ne $newHelpComment) {
            $helpToken | Set-ScriptExtent -Text $newHelpComment
        }

        Start-Sleep -Milliseconds 50

        $targetMarkdownPath = '{0}\{1}.md' -f $docsPath, $Ast.Name
        if (-not (Test-Path $targetMarkdownPath)) {
            $null = New-Item $targetMarkdownPath -ItemType File
        }

        SetEditorLocation $targetMarkdownPath

        # Shape markdown according to linting rules.
        $markdownContent = $markdownContent -replace
            # Add a new line after headers.
            '([#]+) ([ \w\(\)\-_]+)(\r?\n)(?=[\w{`])', '$1 $2$3$3' -replace
            # Add powershell to code start markers.
            '(?<=\r?\n\r?\n)```(?!powershell|yaml)', '```powershell' -replace
            # Remove the trailing spaces from blank Aliases.
            '(?<=Aliases:) (?!\w)' -replace
            # Replace inconsistent example titles.
            '### Example (\d+)', '### -------------------------- EXAMPLE $1 --------------------------'


        WaitUntil { $psEditor.GetEditorContext().CurrentFile.Path -eq $targetMarkdownPath }

        Find-Ast -IncludeStartingAst -First | Set-ScriptExtent -Text $markdownContent
    }
}

