Import-Module $PSScriptRoot/EditorServicesCommandSuite.dll
Import-Module $PSScriptRoot/RefactorCmdlets/RefactorCmdlets.cdxml

if (-not $CommandSuite -or $CommandSuite -isnot [EditorServicesCommandSuite.Internal.CommandSuite]) {
    $IsMainRunspace = $true

    if ($null -ne $psEditor) {
        Add-Type -Path "$PSScriptRoot/EditorServicesCommandSuite.EditorServices.dll"
        try {
            $powerShellContext = [Microsoft.PowerShell.EditorServices.PowerShellContext]::new(
                [EditorServicesCommandSuite.EditorServices.Internal.NullLogger]::Instance,
                $false)
        } catch [System.Management.Automation.MethodException] {
            $powerShellContext = [Microsoft.PowerShell.EditorServices.PowerShellContext]::new(
                [EditorServicesCommandSuite.EditorServices.Internal.NullLogger]::Instance)
        }

        $CommandSuite = [EditorServicesCommandSuite.EditorServices.Internal.CommandSuite]::GetCommandSuite(
            $psEditor,
            $ExecutionContext,
            $Host,
            $powerShellContext)
    } else {
        Add-Type -Path "$PSScriptRoot/EditorServicesCommandSuite.PSReadLine.dll"
        $CommandSuite = [EditorServicesCommandSuite.PSReadLine.Internal.CommandSuite]::GetCommandSuite(
            $ExecutionContext,
            $Host)
    }
} else {
    $IsMainRunspace = $false
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

# Export only the functions using PowerShell standard verb-noun naming.
# Be sure to list each exported functions in the FunctionsToExport field of the module manifest file.
# This improves performance of command discovery in PowerShell.
Export-ModuleMember -Function *-* -Cmdlet *-*
