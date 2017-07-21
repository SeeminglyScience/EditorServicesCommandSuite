# Use this file to debug the module.
Import-Module -Name $PSScriptRoot\module\EditorServicesCommandSuite.psd1 -Force

# Uncomment this to break on exceptions.
#Set-PSBreakpoint -Variable StackTrace -Mode Write

# Uncomment and change these to prep editor context.
#$psEditor.Workspace.OpenFile((Join-Path $psEditor.Workspace.Path 'module\Public\Get-Token.ps1'))
#$psEditor.Workspace.OpenFile($MyInvocation.MyCommand.Path)
#$psEditor.GetEditorContext().SetSelection(17, 12, 17, 12)
#Start-Sleep -Milliseconds 50

# Place editor command to debug below (before the return).

return


# To debug against a specific AST, place the expression here (under the return) and use the commands
# above to prepare context and invoke the test command.
