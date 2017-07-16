using namespace System.Diagnostics.CodeAnalysis

function New-ESCSSettingsFile {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [CmdletBinding()]
    [SuppressMessage('PSAvoidShouldContinueWithoutForce', '',
                     Justification='ShouldContinue is called from a subroutine without CmdletBinding.')]
    param(
        [ValidateNotNullOrEmpty()]
        [string]
        $Path = $psEditor.Workspace.Path,

        [switch]
        $Force
    )
    begin {
        function HandleFileExists($filePath) {
            if (-not (Test-Path $filePath)) {
                return
            }
            $shouldRemove = $Force.IsPresent -or
                            $PSCmdlet.ShouldContinue(
                                $Strings.ShouldReplaceSettingsMessage,
                                $Strings.ShouldReplaceSettingsCaption)

            if ($shouldRemove) {
                Remove-Item $targetFilePath
                return
            }
            $exception = [System.ArgumentException]::new(
                $Strings.SettingsFileExists -f $psEditor.Workspace.Path)
            ThrowError -Exception $exception `
                       -Id        SettingsFileExists `
                       -Category  InvalidArgument `
                       -Target    $targetFilePath
        }
    }
    end {
        $targetFilePath = Join-Path $Path -ChildPath 'ESCSSettings.psd1'
        HandleFileExists $targetFilePath

        try {
            $groupDefinition = Get-Content $PSScriptRoot\..\Templates\SettingsFile.stg -Raw -ErrorAction Stop

            $templateSplat = @{
                Group = (New-StringTemplateGroup -Definition $groupDefinition)
                Name  = 'Base'
                Parameters = @{
                    Settings = $script:DEFAULT_SETTINGS.GetEnumerator()
                    Strings  = [pscustomobject]$Strings
                }
            }

            $content = Invoke-StringTemplate @templateSplat
        } catch {
            ThrowError -Exception ([InvalidOperationException]::new($Strings.TemplateGroupCompileError)) `
                       -Id        TemplateGroupCompileError `
                       -Category  InvalidOperation `
                       -Target    $groupDefinition
        }

        $null = New-Item $targetFilePath -Value $content
        if ($psEditor) {
            SetEditorLocation $targetFilePath
        }
    }
}
