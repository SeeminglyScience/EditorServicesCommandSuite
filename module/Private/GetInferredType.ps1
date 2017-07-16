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
    end {
        if ($Ast.InferredType -and $Ast.InferredType -ne ([object])) { return $Ast.InferredType }

        if ($Ast -is [System.Management.Automation.Language.TypeExpressionAst]) {
            return GetType -TypeName $Ast.TypeName
        }
        $PSCmdlet.WriteVerbose($Strings.InferringFromCompletion)
        $results = [System.Management.Automation.CommandCompletion]::CompleteInput(
            <# ast:            #> $Context.CurrentFile.Ast,
            <# tokens:         #> $Context.CurrentFile.Tokens,
            <# cursorPosition: #> $Ast.Extent.EndScriptPosition,
            <# options:        #> $null
        )
        $type = $results.CompletionMatches[0].ToolTip |
            Select-String '\[([\w\.]*)\]\$' |
            ForEach-Object { $PSItem.Matches.Groups[1].Value } |
            GetType

        if ($type -and $type.ToString() -match 'System.Collections' -or $type.IsArray) {
            $type = $null
        }
        if (-not $type -and $Ast -is [ExtendedMemberExpressionAst]) {
            $type = $Ast.InferredType
        }
        if (-not $type -and $Ast -is [System.Management.Automation.Language.MemberExpressionAst]) {

            $member = GetInferredMember $Ast
            $type = $member.ReturnType, $member.PropertyType, $member.FieldType | Where-Object { $_ }

        }
        # If it's a variable then check for it in scopes relevant to the current workspace.
        if (-not $type -and $Ast -is [System.Management.Automation.Language.VariableExpressionAst]) {
            $PSCmdlet.WriteVerbose($Strings.GettingImportedModules)

            $silent = @{
                ErrorAction   = 'Ignore'
                WarningAction = 'Ignore'
            }
            $workspaceModuleGuids = GetWorkspaceFile |
                Where-Object FullName -match '.psd1$' |
                Test-ModuleManifest @silent |
                Where-Object Guid -NotMatch '^[0-]*$' |
                ForEach-Object Guid

            $workspaceModules = Get-Module | Where-Object Guid -In $workspaceModuleGuids

            $internals = $workspaceModules | ForEach-Object {
                $PSItem.SessionState.GetType().
                    GetProperty('Internal', [BindingFlags]'Instance, NonPublic').
                    GetValue($PSItem.SessionState)
            }
            # If there are no modules in the workspace then grab the global scope. This isn't needed
            # otherwise because enumerating a module's scopes will hit global as well.
            if (-not $internals) {
                $PSCmdlet.WriteVerbose($Strings.CheckingDefaultScope)
                $internals = $ExecutionContext.SessionState.GetType().
                    GetProperty('Internal', [BindingFlags]'Instance, NonPublic').
                    GetValue($ExecutionContext.SessionState)
            }

            foreach ($internal in $internals) {
                $PSCmdlet.WriteVerbose($Strings.EnumeratingScopesForMember)
                $searcher = [ref].Assembly.GetType('System.Management.Automation.VariableScopeItemSearcher').
                    InvokeMember(
                        <# name:       #> '',
                        <# invokeAttr: #> [BindingFlags]'CreateInstance, Instance, Public',
                        <# binder:     #> $null,
                        <# target:     #> $null,
                        <# args:       #> @(
                            <# sessionState: #> $internal,
                            <# lookupPath:   #> $Ast.VariablePath,
                            <# origin:       #> [System.Management.Automation.CommandOrigin]::Runspace
                        )
                    )
                # Enumerate manually because enumerating normally sometimes causes an endless loop
                # with global variables.
                do { $match = $searcher.Current.Value }
                until ($match -or -not $searcher.MoveNext())

                if ($match.Count -gt 1) { $match = $match[0] }

                if ($match) {
                    $type = $match.GetType()
                    $PSCmdlet.WriteVerbose($Strings.VariableFound -f $type)
                    break
                }
            }
        }
        if (-not $type) {
            ThrowError -Exception ([InvalidOperationException]::new($Strings.CannotInferType -f $Ast)) `
                       -Id        CannotInferType `
                       -Category  InvalidOperation `
                       -Target    $Ast
        }
        $type
    }
}
