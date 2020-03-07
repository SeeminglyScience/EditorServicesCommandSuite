[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $Name,

    [Parameter(Mandatory)]
    [version] $RequiredVersion,

    [Parameter()]
    [ValidateSet('CurrentUser', 'AllUsers')]
    [string] $Scope = 'CurrentUser',

    [Parameter()]
    [switch] $Force
)
end {
    if (Get-Module $Name -ErrorAction Ignore) {
        Remove-Module $Name -Force
    }

    $importModuleSplat = @{
        RequiredVersion = $RequiredVersion
        Name = $Name
        ErrorAction = 'Stop'
    }

    # TODO: Install required versions into the tools folder
    try {
        Import-Module @importModuleSplat -Force
    } catch [System.IO.FileLoadException] {
        # Hope this is the FileList manifest bug and move on if the module
        # seems like it loaded.
        if (-not (Get-Module $Name -ErrorAction Ignore)) {
            throw $PSItem
        }
    } catch [System.IO.FileNotFoundException] {
        Install-Module @importModuleSplat -Force:$Force.IsPresent -Scope $Scope
        Import-Module @importModuleSplat -Force
    }
}
