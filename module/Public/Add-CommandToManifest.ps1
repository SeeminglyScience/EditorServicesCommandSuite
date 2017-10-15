using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Collections.Generic
using namespace System.Linq

function Add-CommandToManifest {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [CmdletBinding()]
    [EditorCommand(DisplayName='Add Closest Function To Manifest')]
    param()
    begin {
        function AssertRequiredSetting {
            param([string] $SettingName)
            end {
                if (-not [string]::IsNullOrWhiteSpace($settings.$SettingName)) {
                    return
                }

                $exception = [System.Management.Automation.PSArgumentException]::new(
                    $Strings.InvalidSettingValue -f $SettingName)
                $throwErrorSplat = @{
                    Exception = $exception
                    Category  = 'InvalidArgument'
                    Id        = 'InvalidSettingValue'
                    Target    = $settings
                    Show      = $true
                }

                ThrowError @throwErrorSplat
            }
        }
    }

    end {
        $commandAst = Find-Ast -AtCursor |
            Find-Ast -Ancestor -First -IncludeStartingAst { $PSItem.Name -and $PSItem.Name -match '\w+-\w+'}

        $settings     = GetSettings
        $functionName = $commandAst.Name
        $filePath     = $psEditor.GetEditorContext().CurrentFile.Path

        AssertRequiredSetting -SettingName MainModuleDirectory
        AssertRequiredSetting -SettingName SourceManifestPath
        $fileListEntry = $PSCmdlet.SessionState.Path.NormalizeRelativePath(
            $filePath,
            (ResolveRelativePath $settings.MainModuleDirectory))

        $manifestFile = ResolveRelativePath $settings.SourceManifestPath

        try {
            SetEditorLocation $manifestFile
        } catch {
            $exception = [System.Management.Automation.PSArgumentException]::new(
                $Strings.InvalidSettingValue -f 'SourceManifestPath')
            $throwErrorSplat = @{
                Exception = $exception
                Category  = 'InvalidArgument'
                Id        = 'InvalidSettingValue'
                Target    = $settings
                Show      = $true
            }

            ThrowError @throwErrorSplat
            return
        }

        function GetManifestField ([string]$Name) {
            $field = Find-Ast -First { $PSItem.Value -eq $Name } | Find-Ast -First
            # This transforms a literal string array expression into it's output without invoking.
            $valueString = $field.ToString() -replace '@\(\)' `
                                             -split   '[,\n\s]' `
                                             -replace '['',\s]' `
                                             -match   '.' `
                                             -as      [List[string]]
            # yield
            [PSCustomObject]@{
                Ast    = $field
                Extent = $field.Extent
                Value  = $valueString
            }
        }

        $functions = GetManifestField -Name FunctionsToExport
        $functions.Value.Add($functionName)
        $functions.Value.Sort({ $args[0].CompareTo($args[1]) })
        $functions.Extent | Set-ScriptExtent -Text ([Enumerable]::Distinct($functions.Value)) -AsArray

        $fileList = GetManifestField -Name FileList

        $fileList.Value.Add($fileListEntry)
        $fileList.Value.Sort({ $args[0].CompareTo($args[1]) })
        $fileList.Extent | Set-ScriptExtent -Text ([Enumerable]::Distinct($fileList.Value)) -AsArray
    }
}
