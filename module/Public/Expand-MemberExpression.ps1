using namespace System.Collections.Generic
using namespace System.Management.Automation.Host
using namespace System.Management.Automation.Language
using namespace System.Reflection
using namespace System.Text
using namespace Microsoft.PowerShell
using namespace Microsoft.PowerShell.EditorServices.Extensions

function Expand-MemberExpression {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [EditorServicesCommandSuite.Internal.PSAstRefactor(ResourceVariable='Strings', ResourcePrefix='ExpandMemberExpression')]
    [EditorCommand(DisplayName='Expand Member Expression')]
    [CmdletBinding()]
    param(
        [System.Management.Automation.Language.MemberExpressionAst] $RefactorTarget,

        [System.Management.Automation.Hidden()]
        [switch] $Test
    )
    begin {
        # Test if a member can be resolved with just name and flags. The most common reason this
        # would return false is multiple overloads with the same binding flags.
        function TestMemberIsComplex {
            param([System.Reflection.MemberInfo] $Member)
            end {
                $helper = [MemberExpressionGeneration]::new(
                    $Member,
                    $Member.ReflectedType)

                $helper.WriteBridge()
                $helper.WriteBasicGetArgs()
                $basicExpression = $helper.
                    Builder.
                    Remove(
                        $helper.Builder.Length - 1,
                        1).
                    ToString()
                gci
                try {
                    return [scriptblock]::
                        Create($basicExpression).
                        InvokeReturnAsIs() -isnot
                        [MemberInfo]
                } catch {
                    return $true
                }
            }
        }

        # Creates a member expression using normal PowerShell syntax, plus arguments
        # and parameter name comments.
        function GetPublicMemberExpression {
            param(
                [System.Reflection.MemberInfo] $Member,
                [string] $Expression
            )
            end {
                if ($Member -isnot [MethodBase]) {
                    return $Expression
                }

                $sb = [StringBuilder]::new($Expression).
                    Append('(')

                if ($parameters = $Member.GetParameters()) {
                    $null = $sb.
                        AppendLine().
                        Append([MemberExpressionGeneration]::Indent).
                        Append(
                            [MemberExpressionGeneration]::
                                GetInvocationArgs($parameters) -join (
                                    ',' +
                                    [Environment]::NewLine +
                                    [MemberExpressionGeneration]::Indent))
                }

                $sb.Append(')')
            }
        }

        # Create an invokable expression all ASTs passed including accessing non-public members.
        function GetInvokableExpressions {
            param([MemberExpressionAst[]] $Ast)
            end {
                $nextTarget = $Ast[0].Expression.ToString()
                $lastAst = $Ast[-1]
                foreach ($expression in $Ast) {
                    $isLastAst = $expression -eq $lastAst
                    $targetMember = GetTargetMember -Ast $expression

                    if (-not $targetMember) {
                        $exception = [MissingMemberException]::new(
                            $expression.Expression,
                            $expression.Member.Value)

                        ThrowError -Exception $exception `
                                   -Id        MissingMember `
                                   -Category  InvalidResult `
                                   -Target    $expression `
                                   -Show
                    }

                    if ($expression.Expression.TypeName) {
                        $nextTarget = $targetMember.ReflectedType
                    }

                    $isPrivateTypeConstructor =
                        $targetMember -is [ConstructorInfo] -and -not
                        $targetMember.ReflectedType.IsPublic

                    $isPublic = $targetMember.IsPublic -or $targetMember.GetMethod.IsPublic
                    if ($isPublic -and -not $isPrivateTypeConstructor) {
                        $operator = '.'
                        if ($expression.Expression -is [TypeExpressionAst]) {
                            $useStaticOperator =
                                $targetMember.IsStatic -or
                                $targetMember.GetMethod.IsStatic -or
                                $targetMember -is [ConstructorInfo]
                            if ($useStaticOperator) {
                                $operator = '::'
                            }

                            $nextTarget = [TypeExpressionHelper]::Create($targetMember.ReflectedType)
                        }

                        $nextTarget += $operator + ($targetMember.Name -replace '^\.ctor$', 'new')
                        if ($isLastAst) {
                            return GetPublicMemberExpression -Member $targetMember -Expression $nextTarget
                        }

                        continue
                    }

                    $needsVerbose = TestMemberIsComplex -Member $targetMember

                    $variableExpression = '$' + [TextOps]::ToCamelCase((
                        $targetMember.Name -replace '^\.ctor$', 'new'))

                    $variableExpression + ' = ' + [MemberExpressionGeneration]::GetReflectionExpression(
                        $nextTarget,
                        $targetMember,
                        $needsVerbose)

                    $nextTarget = $variableExpression
                }
            }
        }

        # Infer the MemberInfo of an AST.  Prompt for choice if multiple possible members are found.
        function GetTargetMember {
            param([Ast] $Ast)
            end {
                try {
                    $members = GetInferredMember -Ast $Ast
                } catch {
                    ThrowError -ErrorRecord $PSItem -Show
                }

                if ($members.Count -le 1) {
                    return $members
                }

                if ($property = $members.Where{ $PSItem -is [PropertyInfo] }) {
                    return $property[0]
                }

                [ChoiceDescription[]] $choices = foreach ($member in $members) {
                    $name = $member.Name
                    if ($member -is [MethodBase]) {
                        $name = $name + '(' + $member.GetParameters().Count + ')'
                    }

                    $parameterDescriptions = foreach ($parameter in $member.GetParameters()) {
                        $parameterType = [ToStringCodeMethods]::Type(
                            $parameter.ParameterType)

                        if ($parameterType -match '\.') {
                            $parameterType = $parameter.ParameterType.Name
                        }

                        '[' +
                        $parameterType+
                        '] ' +
                        $parameter.Name
                    }

                    # yield
                    [ChoiceDescription]::new(
                        $name,
                        $parameterDescriptions -join ', ')
                }

                $choice = ReadChoicePrompt -Prompt 'Please specify which overload to use' -Choices $choices

                return $members[$choice]
            }
        }

        # Get all member expressions in a chain in reverse tree order.
        # eg. return ASTs for "$ExecutionContent.SessionState" then "$ExecutionContext.SessionState.Internal"
        function GetMemberExpressions {
            param([MemberExpressionAst] $Ast)
            end {
                $memberExpressions = [List[MemberExpressionAst]]::new()
                $memberExpressions.Add($Ast)

                $currentExpression = $Ast.Expression
                while ($currentExpression -is [MemberExpressionAst]) {
                    $memberExpressions.Add($currentExpression)
                    $currentExpression = $currentExpression.Expression
                }

                $memberExpressions.Reverse()
                return $memberExpressions.ToArray()
            }
        }
    }
    end {
        if ($Test.IsPresent) {
            return $true
        }

        $Ast = $RefactorTarget
        if (-not $Ast) {
            $Ast = Find-Ast -AtCursor
        }

        $targetAst = GetAncestorOrThrow -Ast $Ast -AstTypeName MemberExpressionAst -ShowOnThrow

        $expressions = GetMemberExpressions -Ast $targetAst

        $newExpressions = GetInvokableExpressions -Ast $expressions

        [string] $firstLineIndent = $targetAst.Extent.StartScriptPosition.Line |
            Select-String '^\s+' |
            ForEach-Object { $PSItem.Matches[0].Value }

        $final = $newExpressions -join ([Environment]::NewLine + [Environment]::NewLine) |
            AddIndent -Amount ($firstLineIndent.Length) -ExcludeFirstLine

        $targetAst | Set-ScriptExtent -Text $final
    }
}
