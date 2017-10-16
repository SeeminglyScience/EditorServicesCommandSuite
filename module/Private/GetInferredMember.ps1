using namespace System.Reflection

function GetInferredMember {
    <#
    .SYNOPSIS
        Get inferred member info from a MemberExpressionAst.
    .DESCRIPTION
        This function attempts to infer the class it belongs to with the function GetInferredType.
        Once the class is determined, it looks for members with the same name, preferring properties
        over fields if both are present.

        If a method or constructor is overloaded, the member with the lowest parameter count will be
        chosen by default. If the ast has arguments specified, the parameter count will be checked for
        a match as well. If the ast has one argument specified and it is of the type int, that will
        be checked as the parameter count. (e.g. [exception]::new(2) would return the overload with
        two parameters)
    .INPUTS
        System.Management.Automation.Language.MemberExpressionAst

        You can pass member expressions to this function.
    .OUTPUTS
        System.Reflection.MemberInfo

        The inferred member information will be returned if found.
    .EXAMPLE
        PS C:\> [Parser]::ParseInput('$host.Context').FindAll({$args[0].Member}, $true) | GetInferredMember
        Returns a System.Reflection.MemberInfo object for the context property.
    #>
    [CmdletBinding()]
    param(
        # Specifies the member expression ast to infer member info from.
        [Parameter(Position=0, Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [Alias('MemberExpressionAst', 'Expression')]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.Language.MemberExpressionAst]
        $Ast
    )
    process {
        try {
            $type = GetInferredType -Ast $Ast.Expression
        } catch {
            throw [System.Management.Automation.ErrorRecord]::new(
                <# exception:     #> $exception,
                <# errorId:       #> 'MissingMember',
                <# errorCategory: #> 'InvalidResult',
                <# targetObject:  #> $Ast)
        }

        $member = $type.FindMembers(
            <# memberType:     #> 'All',
            <# bindingAttr:    #> [BindingFlags]'NonPublic, Public, Instance, Static, IgnoreCase',
            <# filter:         #> { param($member, $criteria) $member.Name -eq $criteria },
            <# filterCriteria: #> ($Ast.Member.Value -replace '^new$', '.ctor'))

        if ($member) {
            return $member
        }

        $exception = [System.MissingMemberException]::new($Ast.Expression, $Ast.Member.Value)
        throw [System.Management.Automation.ErrorRecord]::new(
            <# exception:     #> $exception,
            <# errorId:       #> 'MissingMember',
            <# errorCategory: #> 'InvalidResult',
            <# targetObject:  #> $Ast)
    }
}
