function WaitUntil {
    param([scriptblock]$Predicate, [int]$Timeout = 300, [switch]$PassThru)

    $loop = 0
    while (-not $Predicate.Invoke()) {
        Start-Sleep -Milliseconds 50
        $loop += 50
        if ($loop -ge $Timeout) {
            if ($PassThru.IsPresent) {
                return $false
            }
            break
        }
    }
    if ($PassThru.IsPresent) {
        return $true
    }
}
