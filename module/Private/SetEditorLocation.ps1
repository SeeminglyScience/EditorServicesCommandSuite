function SetEditorLocation {
    [CmdletBinding()]
    param([string]$Path)
    end {
        $resolved = ResolveRelativePath $Path
        $psEditor.Workspace.OpenFile($resolved)

        WaitUntil { $psEditor.GetEditorContext().CurrentFile.Path -eq $resolved.Path }
    }
}
