[CmdletBinding()]
param(
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $Version = '2.1.401',

    [Parameter()]
    [switch] $Unix
)
begin {
    function TestDotNetVersion([System.Management.Automation.CommandInfo] $command) {
        $existingVersion = ((& $command --version) -split '-')[0]
        if ($existingVersion -and (([version]$existingVersion) -ge ([version]$Version))) {
            return $true
        }

        return $false
    }

    $targetFolder = "$PSScriptRoot/dotnet"
    $executableName = 'dotnet.exe'
    if ($Unix.IsPresent) {
        $executableName = 'dotnet'
    }
}
end {
    if (($dotnet = Get-Command $executableName -ea 0) -and (TestDotNetVersion $dotnet)) {
        return $dotnet
    }

    $localAppData = [Environment]::GetFolderPath(
        [Environment+SpecialFolder]::LocalApplicationData,
        [Environment+SpecialFolderOption]::Create)

    $localAppData = Join-Path $localAppData -ChildPath 'Microsoft/dotnet'
    if ($dotnet = Get-Command $localAppData/$executableName -ea 0) {
        if (TestDotNetVersion $dotnet) {
            return $dotnet
        }

        # If dotnet is already installed to local AppData but is not the version we are expecting,
        # don't remove it. Instead try to install to the project directory (and check for an
        # existing one).
        if ($dotnet = Get-Command $targetFolder/$executableName -ea 0) {
            if (TestDotNetVersion $dotnet) {
                return $dotnet
            }

            Write-Host -ForegroundColor Yellow Found dotnet $found but require $Version, replacing...
            Remove-Item $targetFolder -Recurse
            $dotnet = $null
        }
    } else {
        # The Core SDK isn't already installed to local AppData, so install there.
        $targetFolder = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($localAppData)
        if (-not (Test-Path $targetFolder)) {
            $null = New-Item $targetFolder -ItemType Directory -Force -ErrorAction Stop
        }
    }

    Write-Host -ForegroundColor Green Downloading dotnet version $Version
    try {
        $installerPath = $null
        if ($Unix.IsPresent) {
            $uri = "https://raw.githubusercontent.com/dotnet/cli/v2.0.0/scripts/obtain/dotnet-install.sh"
            $installerPath = [System.IO.Path]::GetTempPath() + 'dotnet-install.sh'
            $scriptText = [System.Net.WebClient]::new().DownloadString($uri)
            Set-Content $installerPath -Value $scriptText -Encoding UTF8
            $installer = {
                param($Version, $InstallDir)
                end {
                    & (Get-Command bash) $installerPath -Version $Version -InstallDir $InstallDir
                }
            }
        } else {
            $uri = "https://raw.githubusercontent.com/dotnet/cli/v2.0.0/scripts/obtain/dotnet-install.ps1"
            $scriptText = [System.Net.WebClient]::new().DownloadString($uri)

            # Stop the official script from hard exiting at times...
            $safeScriptText = $scriptText -replace 'exit 0', 'return'
            $installer = [scriptblock]::Create($safeScriptText)
        }

        $null = & $installer -Version $Version -InstallDir $targetFolder
    } finally {
        if (-not [string]::IsNullOrEmpty($installerPath) -and (Test-Path $installerPath)) {
            Remove-Item $installerPath -ErrorAction Ignore
        }
    }

    $found = Get-Command $targetFolder/$executableName
    if (-not (TestDotNetVersion $found)) {
        throw 'The dotnet CLI was downloaded without errors but appears to be the incorrect version.'
    }

    return $found
}
