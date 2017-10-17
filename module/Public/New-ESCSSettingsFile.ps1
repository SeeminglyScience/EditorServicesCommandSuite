using namespace System.Diagnostics.CodeAnalysis

function New-ESCSSettingsFile {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [CmdletBinding()]
    [SuppressMessage(
        'PSAvoidShouldContinueWithoutForce', '',
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

        $builder = [System.Text.StringBuilder]::new().Append('@{')

        $null = foreach ($setting in $DEFAULT_SETTINGS.GetEnumerator()) {
            if ($settings.Key -eq 'PreValidated') {
                continue
            }

            $builder.AppendLine().Append([TextOps]::Indent)
            $resourceStringName = 'SettingComment{0}' -f $setting.Key
            if ($Strings.ContainsKey($resourceStringName)) {
                $builder.
                    AppendFormat('# {0}', $Strings[$resourceStringName]).
                    AppendLine().
                    Append([TextOps]::Indent)
            }

            $builder.
                AppendFormat('{0} = ''{1}''', $setting.Key, $setting.Value).
                AppendLine()
        }

        $content = $builder.AppendLine('}').ToString()

        $null = New-Item $targetFilePath -Value $content
        if ($psEditor) {
            try {
                SetEditorLocation $targetFilePath
            } catch [System.Management.Automation.ItemNotFoundException] {
                ThrowError -ErrorRecord $PSItem -Show
            }
        }
    }
}
