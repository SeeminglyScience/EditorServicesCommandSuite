#requires -Module InvokeBuild, PSScriptAnalyzer, Pester, PlatyPS -Version 5.1
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug'
)

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
    $releaseFolder = $Folders.Release
    if (Test-Path $releaseFolder) {
        Remove-Item $releaseFolder -Recurse
    }
    New-Item -ItemType Directory $releaseFolder | Out-Null
    New-Item -ItemType Directory $releaseFolder/RefactorCmdlets | Out-Null

}

task BuildDocs -If { $Discovery.HasDocs } {
    $output = '{0}\{1}' -f $Folders.Release, $PSCulture
    $null = New-ExternalHelp -Path $PSScriptRoot\docs\$PSCulture -OutputPath $output
}

task AssertDependencies AssertPSES, AssertPSRL

task AssertPSES {
    & "$PSScriptRoot\tools\AssertPSES.ps1"
}

task AssertPSRL {
    & "$PSScriptRoot\tools\AssertPSRL.ps1"
}

task AssertPSResGen {
    # Download the ResGen tool used by PowerShell core internally. This will need to be replaced
    # when the dotnet cli gains support for it.
    # The SHA in the uri's are for the 6.0.2 release commit.
    if (-not (Test-Path $PSScriptRoot/tools/ResGen)) {
        New-Item -ItemType Directory $PSScriptRoot/tools/ResGen | Out-Null
    }

    if (-not (Test-Path $PSScriptRoot/tools/ResGen/Program.cs)) {
        $programUri = 'https://raw.githubusercontent.com/PowerShell/PowerShell/36b71ba39e36be3b86854b3551ef9f8e2a1de5cc/src/ResGen/Program.cs'
        Invoke-WebRequest $programUri -OutFile $PSScriptRoot/tools/ResGen/Program.cs -ErrorAction Stop
    }

    if (-not (Test-Path $PSScriptRoot/tools/ResGen/ResGen.csproj)) {
        $projUri = 'https://raw.githubusercontent.com/PowerShell/PowerShell/36b71ba39e36be3b86854b3551ef9f8e2a1de5cc/src/ResGen/ResGen.csproj'
        Invoke-WebRequest $projUri -OutFile $PSScriptRoot/tools/ResGen/ResGen.csproj -ErrorAction Stop
    }
}

task ResGenImpl {
    Push-Location $PSScriptRoot/src/EditorServicesCommandSuite
    try {
        dotnet run --project $PSScriptRoot/tools/ResGen/ResGen.csproj
    } finally {
        Pop-Location
    }
}

task BuildManaged {
    $script:dotnet = $dotnet = & $PSScriptRoot\tools\GetDotNet.ps1 -Unix:$Discover.IsUnix

    & $dotnet publish --framework netstandard2.0 --configuration $Configuration --verbosity q -nologo
}

task BuildRefactorModule {
    $releaseFolder = $Folders.Release
    $dllToImport = "$PSScriptRoot/src/EditorServicesCommandSuite/bin/$Configuration/netstandard2.0/EditorServicesCommandSuite.dll"

    $script = {
        Add-Type -Path '{0}'
        [EditorServicesCommandSuite.Internal.CommandSuite]::WriteRefactorModule('{1}')
    }.ToString() -f $dllToImport, "$releaseFolder\RefactorCmdlets\RefactorCmdlets.cdxml"

    $encodedScript = [convert]::ToBase64String(
        [System.Text.Encoding]::Unicode.GetBytes($script))

    if ('Core' -eq $PSEdition) {
        pwsh -NoProfile -EncodedCommand $encodedScript
    } else {
        powershell -NoProfile -ExecutionPolicy Bypass -EncodedCommand $encodedScript
    }
}

task CopyToRelease  {
    $moduleName = $Settings.Name
    & "$PSScriptRoot\tools\BuildMonolith.ps1" -OutputPath $Folders.Release -ModuleName $Settings.Name

    "$moduleName.psd1",
    'en-US' | ForEach-Object {
        Join-Path $Folders.PowerShell -ChildPath $PSItem |
            Copy-Item -Destination $Folders.Release -Recurse
    }

    $releaseFolder = $Folders.Release
    Copy-Item $PSScriptRoot/src/EditorServicesCommandSuite/bin/$Configuration/netstandard2.0/publish/EditorServicesCommandSuite.* -Destination $releaseFolder
    Copy-Item $PSScriptRoot/src/EditorServicesCommandSuite.EditorServices/bin/$Configuration/netstandard2.0/publish/EditorServicesCommandSuite.* -Destination $releaseFolder
    Copy-Item $PSScriptRoot/src/EditorServicesCommandSuite.PSReadLine/bin/$Configuration/netstandard2.0/publish/EditorServicesCommandSuite.* -Destination $releaseFolder
    Copy-Item $PSScriptRoot/src/EditorServicesCommandSuite.PSReadLine/bin/$Configuration/netstandard2.0/publish/System.Buffers.dll -Destination $releaseFolder
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

task ResGen -Jobs AssertPSResGen, ResGenImpl

task Build -Jobs Clean, AssertDependencies, ResGen, BuildManaged, BuildRefactorModule, CopyToRelease, BuildDocs

task PreRelease -Jobs Build, Analyze, Test

task Install -Jobs PreRelease, DoInstall

task Publish -Jobs PreRelease, DoPublish

task . Build

