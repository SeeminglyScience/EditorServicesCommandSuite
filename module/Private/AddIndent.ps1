function AddIndent {
    [OutputType([string])]
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline)]
        [string[]] $Source,
        [string] $Indent = ' ',
        [int] $Amount = 4,
        [switch] $ExcludeFirstLine
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
        if ($Amount -lt 1) {
            return $sourceText
        }

        $indentText = $Indent * $Amount
        # Preserve new line characters. Only works if not sent a stream.
        $newLine    = [regex]::Match($sourceText, '\r?\n').Value
        $asLines    = $sourceText -split '\r?\n'
        $first      = $true
        $indentedLines = foreach ($line in $asLines) {
            if ($first) {
                $first = $false
                if ($ExcludeFirstLine.IsPresent) {
                    $line
                    continue
                }
            }

            # Don't indent blank lines or here-string ending tags
            $shouldNotIndent = [string]::IsNullOrWhiteSpace($line) -or
                               $line.StartsWith("'@") -or
                               $line.StartsWith('"@')
            if ($shouldNotIndent) {
                $line
                continue
            }

            $indentText + $line
        }

        return $indentedLines -join $newLine
    }
}
