function Invoke-DocumentRefactor {
    [EditorCommand(DisplayName='Get Refactor Options')]
    [CmdletBinding()]
    param()
    end {
        try {
            $null = $CommandSuite.RequestRefactor($PSCmdlet).
                ConfigureAwait($false).
                GetAwaiter().
                GetResult()
        } catch [OperationCanceledException] {
            # Do nothing. This should only be when a menu selection is cancelled, which I'm
            # equating to ^C
        } catch {
            ThrowError $PSItem
        }
    }
}
