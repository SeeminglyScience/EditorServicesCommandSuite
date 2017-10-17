#requires -Module InvokeBuild, PSScriptAnalyzer, Pester, PlatyPS -Version 5.1
[CmdletBinding()]
param()

$moduleName = 'EditorServicesCommandSuite'
$manifest = Test-ModuleManifest -Path $PSScriptRoot\module\$moduleName.psd1 -ErrorAction Ignore -WarningAction Ignore

$script:Settings = @{
    Name          = $moduleName
    Manifest      = $manifest
    Version       = $manifest.Version
    ShouldAnalyze = $true
    ShouldTest    = $true
}

$script:Folders  = @{
    PowerShell = "$PSScriptRoot\module"
    Release    = '{0}\Release\{1}\{2}' -f $PSScriptRoot, $moduleName, $manifest.Version
    Docs       = "$PSScriptRoot\docs"
    Test       = "$PSScriptRoot\test"
    PesterCC   = "$PSScriptRoot\*.psm1", "$PSScriptRoot\Public\*.ps1", "$PSScriptRoot\Private\*.ps1"
}

$script:Discovery = @{
    HasDocs       = Test-Path ('{0}\{1}\*.md' -f $Folders.Docs, $PSCulture)
    HasTests      = Test-Path ('{0}\*.Test.ps1' -f $Folders.Test)
}

task Clean {
    if (Test-Path $PSScriptRoot\Release) {
        Remove-Item $PSScriptRoot\Release -Recurse
    }
    $null = New-Item $Folders.Release -ItemType Directory
}

task BuildDocs -If { $Discovery.HasDocs } {
    $output = '{0}\{1}' -f $Folders.Release, $PSCulture
    $null = New-ExternalHelp -Path $PSScriptRoot\docs\$PSCulture -OutputPath $output
}

task CopyToRelease  {
    $moduleName = $Settings.Name
    & "$PSScriptRoot\tools\BuildMonolith.ps1" -OutputPath $Folders.Release -ModuleName $Settings.Name

    "$moduleName.psd1",
    'en-US' | ForEach-Object {
        Join-Path $Folders.PowerShell -ChildPath $PSItem |
            Copy-Item -Destination $Folders.Release -Recurse
     }
}

task Analyze -If { $Settings.ShouldAnalyze } {
    Invoke-ScriptAnalyzer -Path $Folders.Release -Settings $PSScriptRoot\ScriptAnalyzerSettings.psd1 -Recurse
}

task Test -If { $Discovery.HasTests -and $Settings.ShouldTest } {
    Invoke-Pester -PesterOption @{ IncludeVSCodeMarker = $true }
}

task DoInstall {
    $installBase = $Home
    if ($profile) { $installBase = $profile | Split-Path }
    $installPath = '{0}\Modules\{1}\{2}' -f $installBase, $Settings.Name, $Settings.Version

    if (-not (Test-Path $installPath)) {
        $null = New-Item $installPath -ItemType Directory
    }

    Copy-Item -Path ('{0}\*' -f $Folders.Release) -Destination $installPath -Force -Recurse
}

task DoPublish {
    if (-not (Test-Path $env:USERPROFILE\.PSGallery\apikey.xml)) {
        throw 'Could not find PSGallery API key!'
    }

    $apiKey = (Import-Clixml $env:USERPROFILE\.PSGallery\apikey.xml).GetNetworkCredential().Password
    Publish-Module -Name $Folders.Release -NuGetApiKey $apiKey -Confirm
}

task Build -Jobs Clean, CopyToRelease, BuildDocs

task PreRelease -Jobs Build, Analyze, Test

task Install -Jobs PreRelease, DoInstall

task Publish -Jobs PreRelease, DoPublish

task . Build

