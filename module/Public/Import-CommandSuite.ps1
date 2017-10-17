function Import-CommandSuite {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [CmdletBinding()]
    param()
    end {
        Import-EditorCommand -Module EditorServicesCommandSuite
        if ([string]::IsNullOrWhiteSpace($psEditor.Workspace.Path)) {
            return
        }

        $watcher = [System.IO.FileSystemWatcher]::new(
            <# path:   #> $psEditor.Workspace.Path,
            <# filter: #> 'ESCSSettings.psd1')

        $watcher.NotifyFilter = 'LastWrite, CreationTime, FileName'
        $watcher.EnableRaisingEvents = $true
        $subscribers = & {
            $guid = [guid]::NewGuid().ToString('n')
            $identifier = 'EditorServicesCommandSuite-{0}-{1}'
            $eventSplat = @{
                InputObject  = $watcher
                SupportEvent = $true
            }

            # Register event subscribers to handle changes to the settings file.
            $eventSplat.SourceIdentifier = ($identifier -f $guid, 'Changed')
            $null = Register-ObjectEvent @eventSplat -EventName Changed -Action {
                $module = Get-Module EditorServicesCommandSuite
                $null = & $module { GetSettings -ForceReload }
            }

            $eventSplat.SourceIdentifier = ($identifier -f $guid, 'Created')
            $null = Register-ObjectEvent @eventSplat -EventName Created -Action {
                $module = Get-Module EditorServicesCommandSuite
                $null = & $module { GetSettings -ForceReload }
            }

            $eventSplat.SourceIdentifier = ($identifier -f $guid, 'Deleted')
            $null = Register-ObjectEvent @eventSplat -EventName Deleted -Action {
                $module = Get-Module EditorServicesCommandSuite
                $null = & $module { GetSettings -ForceDefault }
            }

            # yield
            Get-EventSubscriber -SourceIdentifier "EditorServicesCommandSuite-$guid*" -Force
        }

        $script:SETTINGS_WATCHER = $watcher
        $script:SETTINGS_SUBSCRIBERS = $subscribers

        # When the module is removed, remove the editor commands it registered and dispose of
        # event subscribers.
        $ExecutionContext.SessionState.Module.OnRemove = {
            $editorCommands = $ExecutionContext.SessionState.Module.ExportedFunctions.Values | Where-Object {
                $PSItem.ScriptBlock.Ast.Body.ParamBlock.Attributes.TypeName.Name -contains
                    'EditorCommand'
            }

            foreach ($command in $editorCommands) {
                $psEditor.UnregisterCommand($command.Name -replace '-')
            }

            $script:SETTINGS_SUBSCRIBERS | Unregister-Event -Force
            $script:SETTINGS_WATCHER.Dispose()
        }
    }
}
