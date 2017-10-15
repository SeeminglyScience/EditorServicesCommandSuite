function ResolveRelativePath {
    [OutputType([System.Management.Automation.PathInfo])]
    [CmdletBinding()]
    param([string]$Path)
    begin {
        function GetTargetPath {
            param()
            end {
                $basePath = $psEditor.Workspace.Path
                if ([string]::IsNullOrWhiteSpace($basePath)) {
                    $basePath = $PWD.Path
                }

                if ([string]::IsNullOrWhiteSpace($Path)) {
                    return $basePath
                }

                return Join-Path $basePath -ChildPath $Path
            }
        }
    }
    end {
        $targetPath = GetTargetPath -Path $Path
        if (-not $PSCmdlet.SessionState.Path.IsProviderQualified($targetPath)) {
            $targetPath = 'Microsoft.PowerShell.Core\FileSystem::' + $targetPath
        }

        return $PSCmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath($targetPath)
    }
}
