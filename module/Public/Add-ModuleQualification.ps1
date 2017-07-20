using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Management.Automation.Language

function Add-ModuleQualification {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [EditorCommand(DisplayName='Add Module Name to Closest Command')]
    [CmdletBinding()]
    param(
        [System.Management.Automation.Language.Ast]
        $Ast
    )
    begin {
        function InferCommandInfo([string]$commandName) {

            $PSCmdlet.WriteVerbose($Strings.InferringFromSession)
            $getCommand = { $PSCmdlet.SessionState.InvokeCommand.GetCommand($commandName, 'All') }

            # Get new closure puts the script block into a new dynamic module, effectively limiting
            # command lookup to the global scope so private functions of this module aren't picked.
            $command = $getCommand.GetNewClosure().Invoke()

            if ($command) { return $command }

            $PSCmdlet.WriteVerbose($Strings.InferringFromWorkspace)
            try {
                $manifest = GetInferredManifest
                if (($moduleInfo = Get-Module $manifest.Name -ErrorAction Ignore)) {
                    return $moduleInfo.Invoke($getCommand)
                }
                return $manifest.ExportedCommands.$commandName
            } catch {
                $PSCmdlet.WriteVerbose($Strings.VerboseInvalidManifest)
            }
            $PSCmdlet.WriteVerbose('Unable to find command "{0}".' -f $commandName)
        }
    }
    end {
        $Ast = GetAncestorOrThrow $Ast -AstType CommandAst

        $command = InferCommandInfo $Ast.GetCommandName()
        if (-not $command) {
            ThrowError -Exception ([ArgumentException]::new($Strings.CannotInferModule)) `
                       -Id        CannotInferModule `
                       -Category  InvalidArgument `
                       -Target    $Ast.GetCommandName() `
                       -Show
        }

        if (-not $command.ModuleName) {
            $PSCmdlet.WriteVerbose($Strings.CommandNotInModule)
            return
        }

        $newExpression = '{0}\{1}' -f $command.ModuleName, $command.Name
        $Ast.CommandElements[0] | Set-ScriptExtent -Text $newExpression
    }
}
