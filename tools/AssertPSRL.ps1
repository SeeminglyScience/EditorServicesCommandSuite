[CmdletBinding()]
param(
    [ValidateNotNull()]
    [string] $RequiredVersion = '2.0.0-beta1'
)
begin {
    Add-Type -AssemblyName System.IO.Compression
    function SaveEntry {
        param(
            [System.IO.Compression.ZipArchiveEntry] $Entry,
            [string] $Destination
        )
        end {
            if (-not (Test-Path $Destination -PathType Container)) {
                throw 'Destination path must be a directory'
            }

            $Destination = Join-Path (Resolve-Path $Destination) -ChildPath $Entry.Name
            $entryStream = $Entry.Open()
            try {
                $destinationStream = [System.IO.FileStream]::new(
                    <# path:   #> $Destination,
                    <# mode:   #> [System.IO.FileMode]::Create,
                    <# access: #> [System.IO.FileAccess]::Write,
                    <# share:  #> [System.IO.FileShare]::ReadWrite)
                try {
                    $entryStream.CopyTo($destinationStream)
                } finally {
                    $destinationStream.Dispose()
                }
            } finally {
                $entryStream.Dispose()
            }
        }
    }
}
end {
    $psrlFolder = $PSCmdlet.GetUnresolvedProviderPathFromPSPath("$PSScriptRoot/../lib/PSReadLine")

    if (Test-Path $psrlFolder\Microsoft.PowerShell.PSReadLine2.dll) {
        return
    }

    if (-not (Test-Path $psrlFolder)) {
        $null = New-Item $psrlFolder -ItemType Directory -Force
    }

    $version = $RequiredVersion

    $oldSecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol
    try {
        [System.Net.ServicePointManager]::SecurityProtocol = 'Tls, Tls11, Tls12'
        $downloadUri = "https://github.com/lzybkr/PSReadLine/releases/download/v$version/PSReadLine.zip"
        Invoke-WebRequest -UseBasicParsing -Uri $downloadUri -OutFile $psrlFolder/PSReadLine.zip
    } finally {
        [System.Net.ServicePointManager]::SecurityProtocol = $oldSecurityProtocol
    }

    # Why do all this when Expand-Archive exists? Well, it doesn't always resolve for me for some
    # reason.  Still gotta figure that out, but this also lets us pick and choose what we want from
    # the archive anyway.
    $fileStream = [System.IO.FileStream]::new(
        <# path:   #> (Join-Path $psrlFolder -ChildPath 'PSReadLine.zip'),
        <# mode:   #> [System.IO.FileMode]::Open,
        <# access: #> [System.IO.FileAccess]::Read,
        <# share:  #> [System.IO.FileShare]::ReadWrite)

    try {
        $archiveStream = [System.IO.Compression.ZipArchive]::new(
            <# stream: #> $fileStream,
            <# mode:   #> [System.IO.Compression.ZipArchiveMode]::Read)

        try {
            $null = New-Item $psrlFolder -ItemType Directory -Force -ErrorAction Ignore
            foreach($entry in $archiveStream.Entries) {
                if (0 -eq $entry.Length -or $entry.FullName -notmatch '\.dll$') {
                    continue
                }

                SaveEntry -Entry $entry -Destination $psrlFolder
            }
        } finally {
            $archiveStream.Dispose()
        }
    } finally {
        $fileStream.Dispose()
        Remove-Item $psrlFolder\PSReadLine.zip -Force
    }
}
