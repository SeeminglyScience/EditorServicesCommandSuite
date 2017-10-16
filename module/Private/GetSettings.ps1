function GetSettings {
    [CmdletBinding(DefaultParameterSetName='Auto')]
    param(
        [Parameter(ParameterSetName='Auto')]
        [switch] $Auto,

        [Parameter(ParameterSetName='Reload')]
        [switch] $ForceReload,

        [Parameter(ParameterSetName='Default')]
        [switch] $ForceDefault
    )
    end {
        function GetSettingsFile {
            $importLocalizedDataSplat = @{
                BaseDirectory = $psEditor.Workspace.Path
                FileName      = 'ESCSSettings.psd1'
            }

            return Import-LocalizedData @importLocalizedDataSplat
        }

        function GetHashtable {
            if ($script:CSSettings) { return $script:CSSettings }

            if (-not [string]::IsNullOrWhiteSpace($psEditor.Workspace.Path)) {
                $targetPath = Join-Path $psEditor.Workspace.Path -ChildPath 'ESCSSettings.psd1'

                if (Test-Path $targetPath) {
                    return GetSettingsFile
                }
            }

            return $script:DEFAULT_SETTINGS
        }

        $settings = switch ($PSCmdlet.ParameterSetName) {
            Auto { GetHashtable }
            Reload { GetSettingsFile }
            Default { $script:DEFAULT_SETTINGS }
        }

        $script:CSSettings = $settings

        # Ensure all settings have a default value even if not present in user supplied file.
        if ($settings.PreValidated) { return $settings }

        foreach ($setting in $script:DEFAULT_SETTINGS.GetEnumerator()) {
            if (-not ($settings.ContainsKey($setting.Key))) {
                $settings.Add($setting.Key, $setting.Value)
            }
        }

        $settings.PreValidated = $true
        return $settings
    }
}
