[CmdletBinding()]
param(
    [string] $Version = '2.1.302', #'2.1.4',

    [switch] $Unix
)
end {
    $TARGET_FOLDER = "$PSScriptRoot/dotnet"

    if (($dotnet = Get-Command dotnet -ea 0) -and (& $dotnet --version) -eq $Version) {
        return $dotnet
    }


    if ($dotnet = Get-Command $TARGET_FOLDER/dotnet.exe -ea 0) {
        if (($found = & $dotnet --version) -eq $Version) {
            return $dotnet
        }
        Write-Host -ForegroundColor Yellow Found dotnet $found but require $Version, replacing...
        Remove-Item $TARGET_FOLDER -Recurse
        $dotnet = $null
    }

    Write-Host -ForegroundColor Green Downloading dotnet version $Version

    if ($Unix.IsPresent) {
        $uri = "https://raw.githubusercontent.com/dotnet/cli/v2.0.0/scripts/obtain/dotnet-install.sh"
        $installerPath = [System.IO.Path]::GetTempPath() + 'dotnet-install.sh'
        $scriptText = [System.Net.WebClient]::new().DownloadString($uri)
        Set-Content $installerPath -Value $scriptText -Encoding UTF8
        $installer = { param($Version, $InstallDir) & (Get-Command bash) $installerPath -Version $Version -InstallDir $InstallDir }
        $executableName = 'dotnet'
    } else {
        $uri = "https://raw.githubusercontent.com/dotnet/cli/v2.0.0/scripts/obtain/dotnet-install.ps1"
        $scriptText = [System.Net.WebClient]::new().DownloadString($uri)

        # Stop the official script from hard exiting at times...
        $safeScriptText = $scriptText -replace 'exit 0', 'return'
        $installer = [scriptblock]::Create($safeScriptText)
        $executableName = 'dotnet.exe'
    }

    $null = & $installer -Version $Version -InstallDir $TARGET_FOLDER

    return Get-Command $TARGET_FOLDER/$executableName
}
