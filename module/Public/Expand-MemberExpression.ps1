using namespace Antlr4.StringTemplate.Compiler
using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Collections.Generic
using namespace System.Management.Automation.Language
using namespace System.Reflection

function Expand-MemberExpression {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [EditorCommand(DisplayName='Expand Member Expression')]
    [CmdletBinding()]
    param(
        [Parameter(Position=1, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.Language.Ast]
        $Ast = (Find-Ast -AtCursor),

        [ValidateSet('GetMethod', 'InvokeMember', 'VerboseGetMethod', 'GetValue', 'SetValue')]
        [string]
        $TemplateName,

        [switch]
        $NoParameterNameComments
    )
    begin {
        try {
            $groupSource = Get-Content -Raw $PSScriptRoot\..\Templates\MemberExpression.stg
            $group = New-StringTemplateGroup -Definition $groupSource -ErrorAction Stop

            $instance = $group.GetType().
                GetProperty('Instance', [BindingFlags]'Instance, NonPublic').
                GetValue($group)

            $renderer = [MemberExpressionRenderer]::new()
            $instance.RegisterRenderer([string], $renderer)
            $instance.RegisterRenderer([type], [TypeRenderer]::new())
        } catch {
            ThrowError -Exception ([TemplateException]::new($Strings.TemplateGroupCompileError, $null)) `
                       -Id        TemplateGroupCompileError `
                       -Category  InvalidData `
                       -Target    $PSItem
        }
}
    process {
        $context = $psEditor.GetEditorContext()
        $memberExpressionAst = $Ast

        if ($memberExpressionAst -isnot [MemberExpressionAst]) {

            $memberExpressionAst = $Ast | Find-Ast { $PSItem -is [MemberExpressionAst] } -Ancestor -First

            if ($memberExpressionAst -isnot [MemberExpressionAst]) {
                ThrowError -Exception ([InvalidOperationException]::new($Strings.MissingMemberExpressionAst)) `
                           -Id        MissingMemberExpressionAst `
                           -Category  InvalidOperation `
                           -Target    $Ast `
                           -Show
            }
        }
        [Stack[ExtendedMemberExpressionAst]]$expressionAsts = $memberExpressionAst
        if ($memberExpressionAst.Expression -is [MemberExpressionAst]) {
            for ($nested = $memberExpressionAst.Expression; $nested; $nested = $nested.Expression) {
                if ($nested -is [MemberExpressionAst]) {
                    $expressionAsts.Push($nested)
                } else { break }
            }
        }
        [List[string]]$expressions = @()
        while ($expressionAsts.Count -and ($current = $expressionAsts.Pop())) {

            # Throw if we couldn't find member information at any point.
            if (-not ($current.InferredMember)) {
                ThrowError -Exception ([MissingMemberException]::new($current.Expression, $current.Member.Value)) `
                           -Id        MissingMember `
                           -Category  InvalidResult `
                           -Target    $Ast `
                           -Show
            }

            switch ($current.Expression) {
                { $PSItem -is [MemberExpressionAst] } {
                    $variable = $renderer.TransformMemberName($PSItem.InferredMember.Name)
                }
                { $PSItem -is [VariableExpressionAst] } {
                    $variable = $PSItem.VariablePath.UserPath
                }
                { $PSItem -is [TypeExpressionAst] } {
                    $source = $current.InferredMember.ReflectedType
                }
            }
            if ($variable) {
                $source = '${0}' -f $variable

                # We don't want to build out reflection expressions for public members so we chain
                # them together in one of the expressions.
                while (($current.InferredMember.IsPublic            -or
                        $current.InferredMember.GetMethod.IsPublic) -and
                        $expressionAsts.Count) {
                    $source += '.{0}' -f $current.InferredMember.Name

                    if ($current.InferredMember.MemberType -eq 'Method') {
                        $source += '({0})' -f $current.Arguments.Extent.Text
                    }
                    $current = $expressionAsts.Pop()
                }
            }

            if ($psEditor) {
                $scriptFile     = $context.CurrentFile.GetType().
                                                       GetField('scriptFile', 60).
                                                       GetValue($context.CurrentFile)

                $line           = $scriptFile.GetLine($memberExpressionAst.Extent.StartLineNumber)
                $indentOffset   = [regex]::Match($line, '^\s*').Value
            }

            $templateParameters = @{
                ast                  = $current
                source               = $source
                includeParamComments = -not $NoParameterNameComments
            }
            $member = $current.InferredMember

            # Automatically use the more explicit VerboseGetMethod template if building a reflection
            # statement for a method with multiple overloads with the same parameter count.
            $needsVerbose = $member -is [MethodInfo] -and -not
                            $member.IsPublic -and
                            $member.ReflectedType.GetMethods(60).Where{
                                $PSItem.Name -eq $current.InferredMember.Name -and
                                $PSItem.GetParameters().Count -eq $member.GetParameters().Count }.
                                Count -gt 1

            if ($TemplateName -and -not $expressionAsts.Count) {
                $templateParameters.template = $TemplateName
            } elseif ($needsVerbose) {
                $templateParameters.template = 'VerboseGetMethod'
            }
            $expression = Invoke-StringTemplate -Group $group -Name Main -Parameters $templateParameters
            $expressions.Add($expression)
        }

        $result = $expressions -join (,[Environment]::NewLine * 2) `
                               -split '\r?\n' `
                               -join ([Environment]::NewLine + $indentOffset)
        if ($psEditor) {
            Set-ScriptExtent -Extent $memberExpressionAst.Extent `
                             -Text   $result
        } else {
            $result
        }
    }
}
