using namespace System.Management.Automation.Language

function GetAncestorOrThrow {
    [OutputType([System.Management.Automation.Language.Ast])]
    param(
        [System.Management.Automation.Language.Ast]
        $Ast,

        [string]
        $AstTypeName,

        [System.Management.Automation.PSCmdlet]
        $ErrorContext,

        [switch]
        $ShowOnThrow
    )
    end {
        $astType = $AstTypeName -as [type]
        if (-not $astType) {
            $astType = 'System.Management.Automation.Language.' + $AstTypeName -as [type]
        }

        if (-not $Ast) { $Ast = Find-Ast -AtCursor }

        if ($Ast -is $astType) { return $Ast }
        $Ast = Find-Ast -Ast $Ast -Ancestor -First { $PSItem -is $astType }
        if ($Ast) { return $Ast }

        $throwErrorSplat = @{
            Exception = ([ArgumentException]::new($Strings.MissingAst -f $astType.Name))
            Target    = $Ast
            Category  = 'InvalidArgument'
            Id        = 'MissingAst'
            Show      = $ShowOnThrow.IsPresent
        }
        if ($ErrorContext) { $throwErrorSplat.ErrorContext = $ErrorContext }
        ThrowError @throwErrorSplat
    }
}
