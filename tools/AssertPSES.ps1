[CmdletBinding()]
param(
    [ValidateNotNull()]
    [version] $RequiredVersion = '1.7.0'
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
                    # $destinationStream.Flush()
                    # $destinationStream.Close()
                    $destinationStream.Dispose()
                }
            } finally {
                $entryStream.Dispose()
            }
        }
    }
}
end {
    $psesFolder = $PSCmdlet.GetUnresolvedProviderPathFromPSPath("$PSScriptRoot/../lib/PowerShellEditorServices")

    if (Test-Path $psesFolder\bin\Core\*.dll) {
        return
    }

    if (-not (Test-Path $psesFolder)) {
        $null = New-Item $psesFolder -ItemType Directory -Force
    }

    $version = $RequiredVersion.ToString('3')

    $oldSecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol
    try {
        [System.Net.ServicePointManager]::SecurityProtocol = 'Tls, Tls11, Tls12'
        $downloadUri = "https://github.com/PowerShell/PowerShellEditorServices/releases/download/v$version/PowerShellEditorServices.zip"
        Invoke-WebRequest -UseBasicParsing -Uri $downloadUri -OutFile $psesFolder/PowerShellEditorServices.zip
    } finally {
        [System.Net.ServicePointManager]::SecurityProtocol = $oldSecurityProtocol
    }

    # Why do all this when Expand-Archive exists? Well, it doesn't always resolve for me for some
    # reason.  Still gotta figure that out, but this also lets us pick and choose what we want from
    # the archive anyway.
    $fileStream = [System.IO.FileStream]::new(
        <# path:   #> (Join-Path $psesFolder -ChildPath 'PowerShellEditorServices.zip'),
        <# mode:   #> [System.IO.FileMode]::Open,
        <# access: #> [System.IO.FileAccess]::Read,
        <# share:  #> [System.IO.FileShare]::ReadWrite)

    try {
        $archiveStream = [System.IO.Compression.ZipArchive]::new(
            <# stream: #> $fileStream,
            <# mode:   #> [System.IO.Compression.ZipArchiveMode]::Read)

        try {
            $null = New-Item $psesFolder\bin\Desktop -ItemType Directory -Force -ErrorAction Ignore
            $null = New-Item $psesFolder\bin\Core -ItemType Directory -Force -ErrorAction Ignore
            foreach($entry in $archiveStream.Entries) {
                if (0 -eq $entry.Length) {
                    continue
                }

                $isDesktop = $entry.FullName.StartsWith(
                    'PowerShellEditorServices\bin\Desktop',
                    [StringComparison]::OrdinalIgnoreCase)

                if ($isDesktop) {
                    SaveEntry -Entry $entry -Destination $psesFolder\bin\Desktop
                    continue
                }

                $isCore = $entry.FullName.StartsWith(
                    'PowerShellEditorServices\bin\Core',
                    [StringComparison]::OrdinalIgnoreCase)

                if ($isCore) {
                    SaveEntry -Entry $entry -Destination $psesFolder\bin\Core
                }
            }
        } finally {
            $archiveStream.Dispose()
        }
    } finally {
        $fileStream.Dispose()
        Remove-Item $psesFolder\PowerShellEditorServices.zip -Force
    }
}
