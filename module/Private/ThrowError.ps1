using namespace System.Management.Automation

function ThrowError {
    [CmdletBinding()]
    param(
        [Parameter(Position=0, Mandatory, ParameterSetName='New')]
        [ValidateNotNullOrEmpty()]
        [Exception]
        $Exception,

        [Parameter(Position=1, Mandatory, ParameterSetName='New')]
        [ValidateNotNullOrEmpty()]
        [string]
        $Id,

        [Parameter(Position=2, Mandatory, ParameterSetName='New')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ErrorCategory]
        $Category,

        [Parameter(Position=3, ParameterSetName='New')]
        [AllowNull()]
        [object]
        $Target,

        [Parameter(Position=0, Mandatory, ParameterSetName='Rethrow')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ErrorRecord]
        $ErrorRecord,

        [Alias('PSCmdlet')]
        [System.Management.Automation.PSCmdlet]
        $ErrorContext,

        [switch]
        $Show
    )
    end {
        # Need to manually check error action because of calling the error methods from a different
        # cmdlet context. Also reading/setting the error preference variable when the value is "Ignore"
        # throws, so we get it through variable intrinsics.
        $errorPreference = $ExecutionContext.SessionState.PSVariable.GetValue('ErrorActionPreference')
        if ($errorPreference -eq 'Ignore') { return }

        if (-not $ErrorContext) {
            foreach ($frame in (Get-PSCallStack)) {
                if ($frame.Command -eq $MyInvocation.MyCommand.Name) { continue }
                if ($ErrorContext = $frame.GetFrameVariables().PSCmdlet.Value) { break }
            }
            if (-not $ErrorContext) { $ErrorContext = $PSCmdlet }
        }
        if ($PSCmdlet.ParameterSetName -eq 'New') {
            $ErrorRecord = [ErrorRecord]::new($Exception, $Id, $Category, $TargetObject)
        }

        if ($errorPreference -eq 'SilentlyContinue') {
            $ErrorContext.WriteError($ErrorRecord)
            return
        }

        if ($psEditor -and $Show.IsPresent) {
            $psEditor.Window.ShowErrorMessage($ErrorRecord.Exception.Message)
        }

        $ErrorContext.ThrowTerminatingError($ErrorRecord)
    }
}
