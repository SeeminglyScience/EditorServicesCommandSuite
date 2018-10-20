Import-Module $PSScriptRoot/EditorServicesCommandSuite.dll
Import-Module $PSScriptRoot/EditorServicesCommandSuite.RefactorCmdlets.cdxml
Update-FormatData -AppendPath $PSScriptRoot/EditorServicesCommandSuite.format.ps1xml

if ($null -ne $psEditor) {
    Add-Type -Path "$PSScriptRoot/EditorServicesCommandSuite.EditorServices.dll"

    $CommandSuite = [EditorServicesCommandSuite.EditorServices.Internal.CommandSuite]::GetCommandSuite(
        $psEditor,
        $ExecutionContext,
        $Host)
} else {
    Add-Type -Path "$PSScriptRoot/EditorServicesCommandSuite.PSReadLine.dll"
    $CommandSuite = [EditorServicesCommandSuite.PSReadLine.Internal.CommandSuite]::GetCommandSuite(
        $ExecutionContext,
        $Host)
}

function Import-CommandSuite {
    [CmdletBinding()]
    param()
    end {
        if ($null -eq $psEditor) {
            return
        }

        $registerEditorCommandSplat = @{
            Name        = 'Invoke-DocumentRefactor'
            DisplayName = 'Get Context Specific Refactor Options'
            # Use Ast.GetScriptBlock to strip SessionState affinity so PSCmdlet.SessionState
            # reflects the real caller.
            ScriptBlock = { Invoke-DocumentRefactor }.Ast.GetScriptBlock()
        }

        Register-EditorCommand @registerEditorCommandSplat

        Get-RefactorOption | ForEach-Object {
            $registerEditorCommandSplat = @{
                Name        = $PSItem.Command.Name
                DisplayName = $PSItem.Name
                ScriptBlock = [scriptblock]::Create($PSItem.Command.Name)
            }

            Register-EditorCommand @registerEditorCommandSplat
        }
    }
}

function Invoke-DocumentRefactor {
    [CmdletBinding()]
    param()
    end {
        try {
            $null = $CommandSuite.RequestRefactor($PSCmdlet).
                ConfigureAwait($false).
                GetAwaiter().
                GetResult()
        } catch [OperationCanceledException] {
            # Do nothing. This should only be when a menu selection is cancelled, which I'm
            # equating to ^C
        } catch {
            $PSCmdlet.WriteError($PSItem)
        }
    }
}

New-Alias -Name Add-CommandToManifest -Value Register-CommandExport -Force

# Export only the functions using PowerShell standard verb-noun naming.
# Be sure to list each exported functions in the FunctionsToExport field of the module manifest file.
# This improves performance of command discovery in PowerShell.
Export-ModuleMember -Function *-* -Cmdlet *-* -Alias Add-CommandToManifest
