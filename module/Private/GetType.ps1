using namespace System.Management.Automation

function GetType {
    <#
    .SYNOPSIS
        Get a type info object for any nonpublic or public type.
    .DESCRIPTION
        Retrieve type info directly from the assembly if nonpublic or from implicitly casting if public.
    .INPUTS
        System.String

        You can pass type names to this function.
    .OUTPUTS
        System.Type

        Returns a Type object if a match is found.
    .EXAMPLE
        PS C:\> 'System.Management.Automation.SessionStateScope' | GetType
        Returns a Type object for SessionStateScope.
    #>
    [CmdletBinding()]
    param (
        # Specifies the type name to search for.
        [Parameter(Mandatory, ValueFromPipeline)]
        [ValidateNotNullOrEmpty()]
        [string]
        $TypeName
    )
    process {
        $type = $TypeName -as [type]

        if (-not $type) {
            $type = [AppDomain]::CurrentDomain.
                GetAssemblies().
                ForEach{ $PSItem.GetType($TypeName, $false, $true) }.
                Where({ $PSItem }, 'First')[0]
        }

        if (-not $type) {
            $type = [AppDomain]::CurrentDomain.
                GetAssemblies().
                GetTypes().
                Where({ $PSItem.ToString() -match "$TypeName$" }, 'First')[0]
        }
        # TODO: Pull using statements from the ast to catch some edge cases.
        if (-not $type) {
            ThrowError -Exception ([RuntimeException]::new($Strings.TypeNotFound -f $TypeName)) `
                       -Id        TypeNotFound `
                       -Category  InvalidOperation `
                       -Target    $TypeName
            return
        }
        $type
    }
}
