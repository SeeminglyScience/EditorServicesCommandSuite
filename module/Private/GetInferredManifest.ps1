using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Management.Automation

function GetInferredManifest {
    [CmdletBinding()]
    param()
    end {
        $manifestPath = ResolveRelativePath (GetSettings).SourceManifestPath

        if ([string]::IsNullOrWhiteSpace($manifestPath)) {
            throw [ErrorRecord]::new(
                <# exception:     #> [PSInvalidOperationException]::new($Strings.MissingWorkspaceManifest),
                <# errorId:       #> 'MissingWorkspaceManifest',
                <# errorCategory: #> 'InvalidOperation',
                <# targetObject:  #> $manifestPath)
        }

        if (-not (Test-Path $manifestPath)) {
            $exception = [IO.InvalidDataException]::new(
                $Strings.InvalidSettingValue -f 'SourceManifestPath')
            throw [ErrorRecord]::new(
                <# exception:     #> $exception,
                <# errorId:       #> 'InvalidSettingValue',
                <# errorCategory: #> 'InvalidData',
                <# targetObject:  #> $manifestPath)
        }

        $data = Import-LocalizedData -BaseDirectory (Split-Path $manifestPath) `
                                     -FileName      (Split-Path -Leaf $manifestPath)
        $null = $data.Add('Name', ((Split-Path $manifestPath -Leaf) -replace '.psd1$'))
        return $data
    }
}
