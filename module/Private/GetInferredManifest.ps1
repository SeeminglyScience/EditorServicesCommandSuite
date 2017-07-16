using namespace Microsoft.PowerShell.EditorServices.Extensions

function GetInferredManifest {
    [CmdletBinding()]
    param()
    end {
        $manifestPath = ResolveRelativePath (GetSettings).SourceManifestPath
        if (-not $manifestPath -or -not (Test-Path $manifestPath)) {
            ThrowError -Exception ([IO.InvalidDataException]::new($Strings.InvalidManifestSetting)) `
                       -Id        InvalidManifestSetting `
                       -Category  InvalidDataException `
                       -Target    $manifestPath
        }
        $data = Import-LocalizedData -BaseDirectory (Split-Path $manifestPath) `
                                     -FileName      (Split-Path -Leaf $manifestPath)
        $null = $data.Add('Name', ((Split-Path $manifestPath -Leaf) -replace '.psd1$'))
        return $data
    }
}
