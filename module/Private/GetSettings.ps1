function GetSettings {
    [CmdletBinding()]
    param()
    end {
        function GetHashtable {
            if ($script:CSSettings) { return $script:CSSettings }

            $targetPath = Join-Path $psEditor.Workspace.Path -ChildPath 'ESCSSettings.psd1'

            if (Test-Path $targetPath) {
                $script:CSSettings = Import-LocalizedData -BaseDirectory $psEditor.Workspace.Path `
                                                        -FileName 'ESCSSettings.psd1'

                return $script:CSSettings
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
