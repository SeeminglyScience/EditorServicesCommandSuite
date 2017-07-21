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
            # HACK: If someone knows a reliable way to perform command lookup outside the module,
            #       without reflection, please let me know or send a PR.
            $flags = [Reflection.BindingFlags]'Instance, NonPublic'
            $context = $ExecutionContext.
                GetType().
                GetField('_context', $flags).
                GetValue($ExecutionContext)
            $globalState = $context.
                GetType().
                GetProperty('TopLevelSessionState', $flags).
                GetValue($context)

            $getCommand = { $ExecutionContext.InvokeCommand.GetCommand($commandName, 'All') }

            $null = [scriptblock].
                GetProperty('SessionStateInternal', $flags).
                SetValue($getCommand, $globalState)

            $PSCmdlet.WriteVerbose($Strings.InferringFromSession)

            $command = $getCommand.InvokeReturnAsIs()

            if ($command) { return $command }

            $PSCmdlet.WriteVerbose($Strings.InferringFromWorkspace)
            try {
                $manifest = GetInferredManifest
                if (($moduleInfo = Get-Module $manifest.Name -ErrorAction Ignore)) {
                    return $moduleInfo.Invoke(
                        { $ExecutionContext.SessionState.InvokeCommand.GetCommand($args[0], 'All') },
                        $commandName)
                }
                $isExport = $manifest.FunctionsToExport -contains $commandName -or
                            $manifest.CmdletsToExport   -contains $commandName
                # If it's exported in the manifest but not loaded we can't actually get CommandInfo,
                # but we can return the properties we expect anyway.
                if ($isExport) {
                    return @{
                        ModuleName = $manifest.Name
                        Name       = $commandName
                    }
                }
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
