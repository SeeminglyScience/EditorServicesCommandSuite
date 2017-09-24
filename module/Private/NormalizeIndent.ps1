function NormalizeIndent {
    [OutputType([string])]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [ValidateNotNullOrEmpty()]
        [string[]] $Source,

        [ValidateRange(0, [int]::MaxValue)]
        [int] $DecreaseIndentAmount
    )
    begin {
        $stringList = [System.Collections.Generic.List[string]]::new()
    }
    process {
        if ($null -eq $Source) {
            return
        }

        $stringList.AddRange($Source)
    }
    end {
        $sourceText = $stringList -join [Environment]::NewLine
        # Preserve new line characters. Only works if not sent a stream.
        $newLine    = [regex]::Match($sourceText, '\r?\n').Value
        $asLines    = $sourceText -split '\r?\n'

        if (-not $DecreaseIndentAmount) {
            # Get the smallest index of each lines first non-whitespace character. Ignore
            # here string ending tags and lines with only whitespace or nothing.
            $DecreaseIndentAmount = $asLines |
                Select-String "^(?!'@)\s*(\S)" |
                ForEach-Object { $PSItem.Matches[0].Groups[1].Index } |
                Sort-Object |
                Select-Object -First 1
        }

        $asLines -replace "^\s{0,$DecreaseIndentAmount}" -join $newLine
    }
}
