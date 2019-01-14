#requires -Module InvokeBuild -Version 5.1
[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Debug',

    [Parameter()]
    [string] $Framework = 'netstandard2.0',

    [Parameter()]
    [switch] $Force
)

$ModuleName     = 'EditorServicesCommandSuite'
$Culture        = 'en-US'
$ShouldAnalyze  = $false
$ShouldTest     = $true

$FailOnError = @{
    ErrorAction = [System.Management.Automation.ActionPreference]::Stop
}

$Silent = @{
    ErrorAction = [System.Management.Automation.ActionPreference]::Ignore
    WarningAction = [System.Management.Automation.ActionPreference]::Ignore
}

$Manifest       = Test-ModuleManifest -Path "$PSScriptRoot/module/$ModuleName.psd1" @Silent
$Version        = $Manifest.Version
$PowerShellPath = "$PSScriptRoot/module"
$CSharpPath     = "$PSScriptRoot/src"
$ReleasePath    = "$PSScriptRoot/Release/$ModuleName/$Version"
$DocsPath       = "$PSScriptRoot/docs"
$TestPath       = "$PSScriptRoot/test/$ModuleName.Tests"
$ToolsPath      = "$PSScriptRoot/tools"
$HasDocs        = Test-Path $DocsPath/$Culture/*.md
$HasTests       = Test-Path $TestPath/*
$IsUnix         = $PSEdition -eq 'Core' -and -not $IsWindows

task Clean {
    if (Test-Path $ReleasePath) {
        Remove-Item $ReleasePath -Recurse
    }

    New-Item -ItemType Directory $ReleasePath | Out-Null
}

task BuildDocs -If { $HasDocs } {
    New-ExternalHelp -Path $DocsPath/$Culture -OutputPath $ReleasePath/$Culture | Out-Null
}

task AssertDependencies AssertPowerShellCore, AssertRequiredModules, AssertDotNet, AssertPSES, AssertPSRL

task AssertPowerShellCore {
    $script:pwsh = $pwsh = Get-Command pwsh @Silent
    if ($pwsh) {
        return
    }

    if ($Force.IsPresent) {
        choco install powershell-core --version 6.1.1 -y
    } else {
        choco install powershell-core --verison 6.1.1
    }

    $script:pwsh = Get-Command $env:ProgramFiles/PowerShell/6/pwsh.exe @FailOnError
}

task AssertRequiredModules {
    $assertRequiredModule = Get-Command $ToolsPath/AssertRequiredModule.ps1 @FailOnError
    & $assertRequiredModule platyPS -RequiredVersion 0.12.0 -Force:$Force.IsPresent
}

task AssertDotNet {
    $script:dotnet = & $ToolsPath/GetDotNet.ps1 -Unix:$IsUnix
}

task AssertPSES {
    & "$ToolsPath/AssertPSES.ps1"
}

task AssertPSRL {
    & "$ToolsPath/AssertPSRL.ps1"
}

task AssertPSResGen {
    # Download the ResGen tool used by PowerShell core internally. This will need to be replaced
    # when the dotnet cli gains support for it.
    # The SHA in the uri's are for the 6.0.2 release commit.
    if (-not (Test-Path $ToolsPath/ResGen)) {
        New-Item -ItemType Directory $ToolsPath/ResGen | Out-Null
    }

    if (-not (Test-Path $ToolsPath/ResGen/Program.cs)) {
        $programUri = 'https://raw.githubusercontent.com/PowerShell/PowerShell/36b71ba39e36be3b86854b3551ef9f8e2a1de5cc/src/ResGen/Program.cs'
        Invoke-WebRequest $programUri -OutFile $ToolsPath/ResGen/Program.cs @FailOnError
    }

    if (-not (Test-Path $ToolsPath/ResGen/ResGen.csproj)) {
        $projUri = 'https://raw.githubusercontent.com/PowerShell/PowerShell/36b71ba39e36be3b86854b3551ef9f8e2a1de5cc/src/ResGen/ResGen.csproj'
        Invoke-WebRequest $projUri -OutFile $ToolsPath/ResGen/ResGen.csproj @FailOnError
    }
}

task ResGenImpl {
    Push-Location $CSharpPath/$ModuleName
    try {
        & $dotnet run --project $ToolsPath/ResGen/ResGen.csproj
    } finally {
        Pop-Location
    }
}

task BuildManaged {
    & $dotnet publish --framework $Framework --configuration $Configuration --verbosity q -nologo
}

task BuildRefactorModule {
    $dllToImport = "$CSharpPath/$ModuleName/bin/$Configuration/$Framework/publish/$ModuleName.dll"

    $script = {
        Add-Type -Path '{0}'
        [EditorServicesCommandSuite.Internal.CommandSuite]::WriteRefactorModule('{1}')
    }.ToString() -f $dllToImport, "$ReleasePath/$ModuleName.RefactorCmdlets.cdxml"

    $encodedScript = [convert]::ToBase64String(
        [System.Text.Encoding]::Unicode.GetBytes($script))

    if ('Core' -eq $PSEdition) {
        & $pwsh -NoProfile -EncodedCommand $encodedScript
    } else {
        powershell -NoProfile -ExecutionPolicy Bypass -EncodedCommand $encodedScript
    }
}

task CopyToRelease  {
    "$ModuleName.psm1", "$ModuleName.psd1", "$ModuleName.format.ps1xml" | ForEach-Object {
        Join-Path $PowerShellPath -ChildPath $PSItem |
            Copy-Item -Destination $ReleasePath -Recurse
    }

    $srcBase = "$CSharpPath/$ModuleName"
    Copy-Item "$srcBase/bin/$Configuration/$Framework/publish/EditorServicesCommandSuite.*" -Destination $ReleasePath
    Copy-Item "$srcBase.EditorServices/bin/$Configuration/$Framework/publish/EditorServicesCommandSuite.*" -Destination $ReleasePath

    $psrlReleaseItems =
        'EditorServicesCommandSuite.*',
        'System.Buffers.dll',
        'System.Memory.dll',
        'System.Numerics.Vectors.dll',
        'System.Runtime.CompilerServices.Unsafe.dll',
        'System.Collections.Immutable.dll'

    foreach ($releaseItem in $psrlReleaseItems) {
        Copy-Item "$srcBase.PSReadLine/bin/$Configuration/$Framework/publish/$releaseItem" -Destination $ReleasePath
    }
}

task Analyze -If { $ShouldAnalyze } {
    Invoke-ScriptAnalyzer -Path $ReleasePath -Settings $PSScriptRoot/ScriptAnalyzerSettings.psd1 -Recurse
}

task DoTest {
    Push-Location $TestPath
    try {
        & $dotnet restore -nologo --verbosity quiet

        $oldPSModulePath = $env:PSModulePath
        try {
            $realModulePath = Join-Path (Split-Path $pwsh.Path) -ChildPath 'Modules'
            $env:PSModulePath = $env:PSModulePath -replace ([regex]::Escape($PSHome)), $realModulePath
            & $dotnet test `
                --framework netcoreapp2.0 `
                --configuration Test `
                --logger "trx;LogFileName=$PSScriptRoot/TestResults/results.trx" `
                -nologo
        } finally {
            $env:PSModulePath = $oldPSModulePath
        }
    } finally {
        Pop-Location
    }
}

task DoInstall {
    $installBase = $Home
    if ($profile) {
        $installBase = $profile | Split-Path
    }

    $installPath = "$installBase/Modules/$ModuleName/$Version"
    if (-not (Test-Path $installPath)) {
        $null = New-Item $installPath -ItemType Directory
    }

    Copy-Item -Path $ReleasePath/* -Destination $installPath -Force -Recurse
}

task DoPublish {
    if (-not (Test-Path $env:USERPROFILE/.PSGallery/apikey.xml)) {
        throw 'Could not find PSGallery API key!'
    }

    $apiKey = (Import-Clixml $env:USERPROFILE/.PSGallery/apikey.xml).GetNetworkCredential().Password
    Publish-Module -Name $ReleasePath -NuGetApiKey $apiKey -Confirm
}

task ResGen -Jobs AssertPSResGen, ResGenImpl

task Build -Jobs Clean, AssertDependencies, ResGen, BuildManaged, BuildRefactorModule, CopyToRelease, BuildDocs

task Test -Jobs Build, DoTest

task PrePublish -Jobs Test

task Install -Jobs Test, DoInstall

task Publish -Jobs Test, DoPublish

task . Build
