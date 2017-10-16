function SetEditorLocation {
    [CmdletBinding()]
    param([string]$Path)
    end {
        $resolved = ResolveRelativePath $Path

        if (-not (Test-Path $resolved)) {
            $exception = [System.Management.Automation.ItemNotFoundException]::new(
                [Microsoft.PowerShell.Commands.UtilityResources]::PathDoesNotExist -f $resolved)
            throw [System.Management.Automation.ErrorRecord]::new(
                <# exception:     #> $exception,
                <# errorId:       #> 'PathNotFound',
                <# errorCategory: #> 'ObjectNotFound',
                <# targetObject:  #> $resolved)
        }

        $psEditor.Workspace.OpenFile($resolved)

        WaitUntil { $psEditor.GetEditorContext().CurrentFile.Path -eq $resolved.Path }
    }
}
