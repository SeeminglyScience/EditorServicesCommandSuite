[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',

    [Parameter()]
    [switch] $Force
)
end {
    & "$PSScriptRoot\tools\AssertRequiredModule.ps1" InvokeBuild 5.4.2 -Force:$Force.IsPresent
    $invokeBuildSplat = @{
        Task = 'PrePublish'
        File = "$PSScriptRoot/EditorServicesCommandSuite.build.ps1"
        Force = $Force.IsPresent
        Configuration = $Configuration
    }

    Invoke-Build @invokeBuildSplat
}
