function GetSettings {
    [CmdletBinding()]
    param()
    end {
        function GetHashtable {
            if ($script:CSSettings) { return $script:CSSettings }

            if (-not [string]::IsNullOrWhiteSpace($psEditor.Workspace.Path)) {
                $targetPath = Join-Path $psEditor.Workspace.Path -ChildPath 'ESCSSettings.psd1'

                if (Test-Path $targetPath) {
                    $importLocalizedDataSplat = @{
                        BaseDirectory = $psEditor.Workspace.Path
                        FileName      = 'ESCSSettings.psd1'
                    }

                    $script:CSSettings = Import-LocalizedData @importLocalizedDataSplat
                    return $script:CSSettings
                }
            }

            $script:CSSettings = $script:DEFAULT_SETTINGS

            return $script:CSSettings
        }

        $settings = GetHashtable

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
