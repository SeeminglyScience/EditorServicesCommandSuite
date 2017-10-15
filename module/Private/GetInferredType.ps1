using namespace System.Reflection

function GetInferredType {
    <#
    .SYNOPSIS
        Attempts to determine the type of a variable within a script file.
    .DESCRIPTION
        This function first attempts to infer type from command completion context.  Failing that
        it will enumerate the scopes of any modules contained in the workspace as well as the global
        scope. If the variable is found in one of the scopes, the type of it's value will be returned.

        If the type cannot be inferred from command completion and the variable is defined in a function
        (or other child scope) this method will not work. The variable needs to be in a scope that
        exists at the time this function is ran. A workaround is to set a breakpoint right after the
        variable is defined.
    .INPUTS
        None
    .OUTPUTS
        System.Type

        Returns the inferred type if it was determined.  This function does not have output otherwise.
    .EXAMPLE
        PS C:\> GetInferredType -Ast $memberExpressionAst.Expression
        Determines the type of the variable used in a member expression.
    #>
    [CmdletBinding()]
    [OutputType([type])]
    param(
        # Specifies the current context of the editor.
        [Parameter(Position=0)]
        [ValidateNotNullOrEmpty()]
        [Microsoft.PowerShell.EditorServices.Extensions.EditorContext]
        $Context = $psEditor.GetEditorContext(),

        # Specifies the ast to analyze.
        [Parameter(Position=1, Mandatory)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.Language.Ast]
        $Ast
    )
    begin {
        function GetInferredTypeImpl {
            # Return cached inferred type if it's our custom MemberExpressionAst
            if ($Ast.InferredType -and $Ast.InferredType -ne [object]) {
                return $Ast.InferredType
            }

            if ($Ast -is [System.Management.Automation.Language.TypeExpressionAst]) {
                return GetType -TypeName $Ast.TypeName
            }

            if ($Ast.StaticType -and $Ast.StaticType -ne [object]) {
                return $Ast.StaticType
            }

            $PSCmdlet.WriteDebug("TYPEINF: Starting engine inference")
            try {
                $flags = [BindingFlags]'Instance, NonPublic'
                $mappedInput = [System.Management.Automation.CommandCompletion]::
                    MapStringInputToParsedInput(
                        $Ast.Extent.StartScriptPosition.GetFullScript(),
                        $Ast.Extent.EndOffset)

                # If anyone knows a public way to go about getting the type inference from the engine
                # give me a shout.
                $analysis = [ref].
                    Assembly.
                    GetType('System.Management.Automation.CompletionAnalysis').
                    InvokeMember(
                        <# name:       #> $null,
                        <# invokeAttr: #> $flags -bor [BindingFlags]::CreateInstance,
                        <# binder:     #> $null,
                        <# target:     #> $null,
                        <# args: #> @(
                            <# ast:            #> $mappedInput.Item1,
                            <# tokens:         #> $mappedInput.Item2,
                            <# cursorPosition: #> $mappedInput.Item3,
                            <# options:        #> @{}))

                $engineContext = $ExecutionContext.GetType().
                    GetField('_context', $flags).
                    GetValue($ExecutionContext)

                $completionContext = $analysis.GetType().
                    GetMethod('CreateCompletionContext', $flags).
                    Invoke($analysis, @($engineContext))

                $type = $Ast.GetType().
                    GetMethod('GetInferredType', $flags).
                    Invoke($Ast, @($completionContext)).
                    Where({ $null -ne $PSItem.Type -and $PSItem.Type -ne [object]}, 'First')[0].
                    Type

                if ($type) {
                    return $type
                }

            } catch {
                $PSCmdlet.WriteDebug('TYPEINF: Engine failed with error ID "{0}"' -f $Error[0].FullyQualifiedErrorId)
            }

            if ($Ast -is [System.Management.Automation.Language.MemberExpressionAst]) {
                $PSCmdlet.WriteDebug('TYPEINF: Starting member inference')
                try {
                    $member = GetInferredMember -Ast $Ast
                } catch [MissingMemberException] {
                    $PSCmdlet.WriteDebug("Couldn't find member for AST $Ast")
                }

                if ($member) {
                    return (
                        $member.ReturnType,
                        $member.PropertyType,
                        $member.FieldType
                    ).Where({ $PSItem -is [type] }, 'First')[0]
                }
            }

            if ($Ast -is [System.Management.Automation.Language.VariableExpressionAst]) {
                $PSCmdlet.WriteDebug('TYPEINF: Starting module state inference')
                try {
                    $inferredManifest = GetInferredManifest
                } catch {
                    $PSCmdlet.WriteVerbose($Strings.VerboseInvalidManifest)
                }

                if ($inferredManifest) {
                $moduleVariable = Get-Module |
                    Where-Object Guid -eq $inferredManifest.GUID |
                    ForEach-Object { $PSItem.SessionState.PSVariable.GetValue($Ast.VariablePath.UserPath) } |
                    Where-Object { $null -ne $PSItem }

                if ($moduleVariable) {
                    return $moduleVariable.Where({ $null -ne $PSItem }, 'First')[0].GetType()
                }
                }


                $PSCmdlet.WriteDebug('TYPEINF: Starting global state inference')

                # I'd rather this use Module.GetVariableFromCallersModule but it appears to throw
                # when a frame in the call stack doesn't have a session state, like the scriptblock
                # of a editor command in some cases.
                $getVariableSplat = @{
                    Scope       = 'Global'
                    Name        = $Ast.VariablePath.UserPath
                    ErrorAction = 'Ignore'
                }

                $foundInGlobal = Get-Variable @getVariableSplat

                if ($foundInGlobal -and $null -ne $foundInGlobal.Value) {
                    return $foundInGlobal.Value.GetType()
                }
            }
        }
    }
    end {
        $type = GetInferredTypeImpl
        if (-not $type) {
            ThrowError -Exception ([InvalidOperationException]::new($Strings.CannotInferType -f $Ast)) `
                       -Id        CannotInferType `
                       -Category  InvalidOperation `
                       -Target    $Ast
            return
        }

        $type
    }
}
