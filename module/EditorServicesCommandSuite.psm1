Import-Module $PSScriptRoot/EditorServicesCommandSuite.dll
Import-Module $PSScriptRoot/EditorServicesCommandSuite.RefactorCmdlets.cdxml
Update-FormatData -AppendPath $PSScriptRoot/EditorServicesCommandSuite.format.ps1xml

if ($null -ne $psEditor) {
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $extensionService = [Microsoft.PowerShell.EditorServices.Extensions.EditorObjectExtensions, Microsoft.PowerShell.EditorServices]::
            GetExtensionServiceProvider($psEditor)

        $assembly = $extensionService.LoadAssemblyInPsesLoadContext((
            Join-Path $PSScriptRoot -ChildPath 'EditorServicesCommandSuite.EditorServices.dll'))

        $type = $assembly.GetType('EditorServicesCommandSuite.EditorServices.Internal.CommandSuite')
    } else {
        Add-Type -Path "$PSScriptRoot/EditorServicesCommandSuite.EditorServices.dll"
        $type = [EditorServicesCommandSuite.EditorServices.Internal.CommandSuite]
    }

    $CommandSuite = $type::GetCommandSuite(
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

        $alreadyLoadedCommands = [System.Collections.Generic.HashSet[string]]::new()
        $psEditor.GetCommands() | ForEach-Object {
            $null = $alreadyLoadedCommands.Add($PSItem.Name)
        }

        Get-RefactorOption | ForEach-Object {
            if ($alreadyLoadedCommands.Contains($PSItem.Id)) {
                return
            }

            $registerEditorCommandSplat = @{
                Name        = $PSItem.Id
                DisplayName = $PSItem.DisplayName
                ScriptBlock = [scriptblock]::Create($PSItem.Id)
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
            $null = $null
        } catch {
            $PSCmdlet.WriteError($PSItem)
        }
    }
}

New-Alias -Name Add-CommandToManifest -Value Register-CommandExport -Force

# Allow the user to opt out of automatic editor command registration on module import.
if (1 -ne $env:ESCS_REQUIRE_EXPLICIT_IMPORT) {
    Import-CommandSuite
}

# Export only the functions using PowerShell standard verb-noun naming.
# Be sure to list each exported functions in the FunctionsToExport field of the module manifest file.
# This improves performance of command discovery in PowerShell.
Export-ModuleMember -Function *-* -Cmdlet *-* -Alias Add-CommandToManifest
