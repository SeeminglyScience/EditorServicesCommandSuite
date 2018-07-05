[CmdletBinding()]
param(
    [string] $Name,

    [version] $RequiredVersion,

    [switch] $Force
)
end {
    if (Get-Module $Name) {
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
    } catch [System.IO.FileNotFoundException] {
        Install-Module @importModuleSplat -Force:$Force.IsPresent -Scope CurrentUser
        Import-Module @importModuleSplat -Force
    }
}
