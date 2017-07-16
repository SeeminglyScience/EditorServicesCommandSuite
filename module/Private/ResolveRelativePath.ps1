function ResolveRelativePath {
    [OutputType([System.Management.Automation.PathInfo])]
    [CmdletBinding()]
    param([string]$Path)
    end {
        if ($resolved = (Resolve-Path (Join-Path $psEditor.Workspace.Path $Path) -ErrorAction Ignore)) {
            return $resolved
        }
        return Resolve-Path $Path
    }
}
