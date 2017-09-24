using namespace System.Management.Automation
using namespace System.Management.Automation.Language
using namespace Microsoft.PowerShell.EditorServices.Extensions

function ConvertTo-FunctionDefinition {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [EditorCommand(DisplayName='Create New Function From Selection')]
    [CmdletBinding(DefaultParameterSetName='__AllParameterSets')]
    param(
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.Language.IScriptExtent] $Extent,

        [ValidateNotNullOrEmpty()]
        [string] $FunctionName,

        [Parameter(ParameterSetName='ExternalFile')]
        [ValidateNotNull()]
        [string] $DestinationPath,

        [Parameter(ParameterSetName='BeginBlock')]
        [switch] $BeginBlock,

        [Parameter(ParameterSetName='Inline')]
        [switch] $Inline
    )
    begin {
        # Ensure a script extent includes the entire starting line including whitespace.
        function ExpandExtent {
            param(
                [Parameter(ValueFromPipeline)]
                [IScriptExtent] $ExtentToExpand
            )
            process {
                if (-not $ExtentToExpand -or $ExtentToExpand.StartColumnNumber -eq 1) {
                    return $ExtentToExpand
                }

                return [Microsoft.PowerShell.EditorServices.FullScriptExtent]::new(
                    $psEditor.GetEditorContext().CurrentFile,
                    [Microsoft.PowerShell.EditorServices.BufferRange]::new(
                        $ExtentToExpand.StartLineNumber,
                        1,
                        $ExtentToExpand.EndLineNumber,
                        $ExtentToExpand.EndColumnNumber))
            }
        }

        # Create an named end block from the default unnamed end block.
        function CreateEndBlock {
            param([NamedBlockAst] $Ast)
            end {
                $statements = $Ast.Statements | Join-ScriptExtent
                $endBlockIndent = $statements.StartColumnNumber - 1
                $statements = $statements | ExpandExtent

                $endBlockText = 'end {',
                                ($statements | NormalizeIndent | AddIndent -Amount 4),
                                '}' |
                                AddIndent -Amount $endBlockIndent

                $statements | Set-ScriptExtent -Text $endBlockText
            }
        }

        # Get specified extent, selected text, or throw.
        function GetTargetExtent {
            if ($Extent) {
                return $Extent | ExpandExtent
            }

            $selectedRange = $psEditor.GetEditorContext().SelectedRange
            if ($selectedRange.Start -ne $selectedRange.End) {
                return $selectedRange | ConvertTo-ScriptExtent | ExpandExtent
            }

            ThrowError -Exception ([PSArgumentException]::new($Strings.NoExtentSelected)) `
                       -Id NoExtentSelected `
                       -Category InvalidArgument `
                       -Target $Extent `
                       -Show
        }

        # Prompt for destination if not specified, throw if no selection is made.
        function ValidateDestination {
            if ($PSCmdlet.ParameterSetName -in 'BeginBlock', 'Inline', 'ExternalFile') {
                return $PSCmdlet.ParameterSetName
            }

            $choices = [Host.ChoiceDescription]::new('BeginBlock', $Strings.ExportFunctionBeginDescription),
                       [Host.ChoiceDescription]::new('Inline', $Strings.ExportFunctionInlineDescription),
                       [Host.ChoiceDescription]::new('ExternalFile', $Strings.ExportFunctionExternalFileDescription)

            $choice = ReadChoicePrompt -Prompt $Strings.ExportFunctionPrompt -Choices $choices
            return $choices[$choice].Label
        }

        # Prompt for file path if selected from the menu, throw if not specified.
        function ValidateDestinationFile {
            if (-not [string]::IsNullOrWhiteSpace($DestinationPath)) {
                return $DestinationPath
            }

            $file = ReadInputPrompt -Prompt $Strings.EnterDestinationFilePrompt
            if (-not [string]::IsNullOrWhiteSpace($file)) {
                return $file
            }

            ThrowError -Exception ([PSArgumentException]::new($Strings.NoDestinationFile)) `
                       -Id NoDestinationFile `
                       -Category InvalidArgument `
                       -Target $file `
                       -Show
        }

        # Prompt for function name if not specified in parameters. Throw if still null.
        function ValidateFunctionName {
            if (-not [string]::IsNullOrWhitespace($FunctionName)) {
                return $FunctionName
            }

            $FunctionName = ReadInputPrompt -Prompt $Strings.ExportFunctionNamePrompt

            if (-not [string]::IsNullOrWhiteSpace($FunctionName)) {
                return $FunctionName
            }

            ThrowError -Exception ([PSArgumentException]::new($Strings.MissingFunctionName)) `
                       -Id MissingFunctionName `
                       -Category InvalidArgument `
                       -Target $FunctionName `
                       -Show
        }

        # Safely captialize the first character if a string. If the string is two or less characters
        # then capitialize the whole string.
        function ToPascalCase {
            param([string] $String)
            end {
                if ($String.Length -le 2) {
                    return $String.ToUpperInvariant()
                }

                return $String.Substring(0, 1).ToUpperInvariant() +
                       ($String[1..$String.Length] -join '')
            }
        }

        # Compile a dictionary of unique variables that should be parameters, along with their
        # inferred type if possible.
        function GetInferredParameters {
            param([VariableExpressionAst[]] $Variables)
            end {
                $parameters = [System.Collections.Generic.Dictionary[string, Tuple[string, string, type]]]::new(
                    [StringComparer]::InvariantCultureIgnoreCase)

                if (-not $Variables.Count) {
                    return $parameters
                }

                foreach ($variable in $Variables) {
                    $asPascalCase = ToPascalCase $variable.VariablePath.UserPath

                    $existingParameter = $null
                    if ($parameters.TryGetValue($asPascalCase, [ref]$existingParameter)) {
                        if ($existingParameter.Item3 -ne [object]) {
                            continue
                        }

                        $inferredType = GetInferredType -Ast $variable -ErrorAction Ignore
                        if ($inferredType -ne [object]) {
                            $parameters[$asPascalCase] = [Tuple[string, string, type]]::new(
                                $asPascalCase,
                                $variable.VariablePath.UserPath,
                                $inferredType)
                        }

                        continue
                    }

                    $inferredType = GetInferredType -Ast $variable -ErrorAction Ignore
                    if (-not $inferredType) {
                        $inferredType = [object]
                    }

                    $parameters.Add(
                        $asPascalCase,
                        [Tuple[string, string, type]]::new(
                            $asPascalCase,
                            $variable.VariablePath.UserPath,
                            $inferredType))
                }

                return $parameters
            }
        }

        # Get variable names for the scope that are considered for our purposes as "locals".
        # Include variables that are:
        # 1 - Assigned within in the target AST
        # 2 - Assigned from language constructs like foreach statements
        # 3 - Special variables like $_/$ExecutionContext/etc
        # 4 - Have a scope in the user path (i.e $global:varName)
        function GetLocalVariables {
            param([Ast] $Ast)
            end {
                $localVariables = [System.Collections.Generic.List[string]]::new()
                $assignmentAsts = Find-Ast -Ast $targetAst -Family {
                    # Find variable assignments, exlude member/index expression assignments.
                    $PSItem -is [AssignmentStatementAst] -and (
                    $PSItem.Left -is [VariableExpressionAst] -or (
                    $PSItem.Left -is [ConvertExpressionAst] -and
                    $PSItem.Left.Child -is [VariableExpressionAst]))
                }

                if ($assignmentAsts.Count) {
                    $assignmentAsts.Left.ForEach{
                        if ($PSItem -is [VariableExpressionAst]) {
                            $localVariables.Add($PSItem.VariablePath.UserPath)
                            return
                        }

                        $localVariables.Add($PSItem.Child.VariablePath.UserPath)
                    }
                }

                $forEachStatements = Find-Ast -Ast $targetAst -Family { $PSItem -is [ForEachStatementAst] }
                if ($forEachStatements.Count) {
                    $localVariables.AddRange(
                        $forEachStatements.Variable.VariablePath.UserPath -as [string[]])
                }

                return $localVariables
            }
        }

        # Create the function definition expression.
        function NewFunctionDefinition {
            end {
                $function = [System.Text.StringBuilder]::new()
                $null = & {
                    $indent = '    '
                    $function.
                        AppendFormat('function {0} {{', $FunctionName).
                        AppendLine().
                        Append($indent).
                        Append('param(')

                    $paramText = $parameters.Values.ForEach{
                        $parameterType = [Microsoft.PowerShell.ToStringCodeMethods]::Type($PSItem.Item3)

                        # Ensure the parameter type is not too generic and is resolvable.
                        if ($parameterType -ne 'System.Object' -and $parameterType -as [type]) {
                            return '[{0}] ${1}' -f $parameterType, $PSItem.Item1
                        }

                        return '${0}' -f $PSItem.Item1
                    }

                    if ($paramText.Count) {
                        $shouldMultiline = $paramText.Count -gt 3
                        $delim = ', '
                        if ($shouldMultiline) {
                            $function.AppendLine().Append($indent + $indent)
                            $delim = ',', [Environment]::NewLine, $indent, $indent -join ''
                        }

                        $function.Append($paramText -join $delim)

                        if ($shouldMultiline) {
                            $function.AppendLine().Append($indent)
                        }
                    }

                    $function.
                        AppendLine(')').
                        Append($indent).
                        AppendLine('end {')

                    $targetWithCorrections = [System.Text.StringBuilder]::new($targetExtent.Text)

                    $targetStartOffset = $targetExtent.StartOffset
                    foreach ($expression in $variableExpressions) {
                        $variableName = $expression.VariablePath.UserPath
                        $asPascalCase = ToPascalCase $variableName
                        $targetWithCorrections.
                            Remove(
                                $expression.Extent.StartOffset - $targetStartOffset,
                                $variableName.Length).
                            Insert(
                                $expression.Extent.StartOffset - $targetStartOffset,
                                $asPascalCase)
                    }

                    $targetWithIndent = $targetWithCorrections |
                        NormalizeIndent |
                        AddIndent -Amount 8

                    $function.
                        AppendLine($targetWithIndent).
                        Append($indent).
                        AppendLine('}').
                        Append('}')
                }

                return $function.ToString()
            }
        }

        # Handle exporting the generated function to an external file.
        function ExportFunctionExternalFile {
            param()
            end {
                $currentFolder = [System.IO.Path]::GetDirectoryName(
                    $psEditor.GetEditorContext().CurrentFile.Path)

                $path = $PSCmdlet.SessionState.Path
                $targetFile = $path.
                    GetUnresolvedProviderPathFromPSPath(
                        $path.Combine(
                            $currentFolder,
                            $targetFile))

                if (-not [System.IO.Path]::GetExtension($targetFile)) {
                    $targetFile = Join-Path $targetFile "$FunctionName.ps1"
                }

                if (-not (Test-Path $targetFile)) {
                    $directory = Split-Path $targetFile
                    if (-not (Test-Path $directory)) {
                        $null = New-Item $directory -ItemType Directory -Force
                    }

                    $null = New-Item $targetFile -ItemType File
                }

                $targetFile = Resolve-Path $targetFile

                $psEditor.Workspace.OpenFile($targetFile)
                WaitUntil { $psEditor.GetEditorContext().CurrentFile.Path -eq $targetFile }

                $psEditor.GetEditorContext().CurrentFile.InsertText($functionText)
            }
        }

        # Handle exporting the generated function to the line directly above the selection.
        function ExportFunctionInline {
            param()
            end {
                $indentedFunction = $functionText | AddIndent -Amount ($targetExtentIndent - 1)
                $psEditor.GetEditorContext().CurrentFile.InsertText(
                    ($indentedFunction + [Environment]::NewLine + [Environment]::NewLine),
                    $targetExtent.StartLineNumber,
                    1)
            }
        }

        # Handle exporting the generated function to the begin block of the closest ancestor function
        # definition.  If there is no ancestor function definition then export to the begin block of
        # the main script AST. This also handles creating a begin block if it doesn't exit, and creating
        # a named end block if there are no named blocks.
        function ExportFunctionBegin {
            param()
            end {
                $findAstSplat = @{
                    Ast          = $targetAst
                    Ancestor     = $true
                    FilterScript = { $PSItem -is [FunctionDefinitionAst] }
                }

                # Find the parent function definition from before we removed the target extent
                $targetBlock = Find-Ast @FindAstSplat | ForEach-Object Body
                if (-not $targetBlock) {
                    $targetBlock = $psEditor.GetEditorContext().CurrentFile.Ast
                }

                if ($targetBegin = $targetBlock.BeginBlock) {
                    $beginIndent = $targetBegin.Extent.StartColumnNumber + 3
                    $entryColumn = ($fullScript -split '\r?\n')[
                        $targetBegin.Extent.StartLineNumber - 1].
                        IndexOf('{') + 2

                    $indentedFunctionText = $functionText | AddIndent -Amount $beginIndent
                    $psEditor.GetEditorContext().CurrentFile.InsertText(
                        [Environment]::NewLine + $indentedFunctionText,
                        $targetBegin.Extent.StartLineNumber,
                        $entryColumn)
                    return
                }

                if ($targetBlock.EndBlock.Unnamed) {
                    # We have to wrap the unnamed block, so we need to get the updated AST. If the block wasn't
                    # nested then we already have the new one.
                    if ($targetBlock.Parent -is [FunctionDefinitionAst]) {
                        $targetBlock = Find-Ast -First {
                            $PSItem -is [ScriptBlockAst] -and
                            $PSItem.Parent -is [FunctionDefinitionAst] -and
                            $PSItem.Extent.StartOffset -eq $targetBlock.Extent.StartOffset
                        }
                    }

                    CreateEndBlock -Ast $targetBlock.EndBlock
                }

                $beginText = 'begin {',
                             (AddIndent -Source $FunctionText),
                             '}' -join
                             [Environment]::NewLine

                $fullScriptAsLines = $fullScript -split '\r?\n'

                [int] $parentBlockIndent = $fullScriptAsLines[$targetBlock.Extent.StartLine - 1] |
                    Select-String '^\s+' |
                    ForEach-Object { $PSItem.Matches[0].Length }

                $entryLine = $targetBlock.Extent.StartLineNumber
                if ($targetBlock.ParamBlock) {
                   $entryLine = $targetBlock.ParamBlock.Extent.EndLineNumber
                }

                $beginIndent = $parentBlockIndent + 4
                $parentIsRoot = -not $targetBlock.Parent
                if ($parentIsRoot) {
                    $beginIndent = 0
                    if (-not $targetBlock.ParamBlock) {
                        $entryColumn = 1
                        $beginText = $beginText + [Environment]::NewLine
                    }
                }

                if (-not $parentIsRoot -or $targetBlock.ParamBlock) {
                    $beginText = [Environment]::NewLine + $beginText
                    $entryColumn = $fullScriptAsLines[$entryLine - 1].Length + 1
                }

                $beginText = AddIndent $beginText -Amount $beginIndent

                $psEditor.GetEditorContext().CurrentFile.InsertText(
                    $beginText,
                    $entryLine,
                    $entryColumn)
            }
        }
    }
    end {
        $FunctionName = ValidateFunctionName
        $targetExtent = GetTargetExtent

        [int] $targetExtentIndent = [regex]::Match(($targetExtent.Text -replace '\r?\n'), '\S').Index

        $fullScript = $targetExtent.StartScriptPosition.GetFullScript()

        # Add braces to the selection so we can have a single AST to use for analysis.
        $alteredScript = $fullScript.
            Insert($targetExtent.EndOffset, '}').
            Insert($targetExtent.StartOffset, '{')

        $scriptAst = [Parser]::ParseInput(
            $alteredScript,
            $targetExtent.File,
            [ref]$null,
            [ref]$null)

        $targetAst = Find-Ast -Ast $scriptAst -First {
            $PSItem.Extent.StartOffset -eq $targetExtent.StartOffset
        }

        $localVariables = GetLocalVariables -Ast $targetAst

        $variableExpressions = Find-Ast -Ast $targetAst -Family {
            $PSItem -is [VariableExpressionAst] -and
            $PSItem.VariablePath.IsUnscopedVariable -and
            $PSItem.VariablePath.UserPath -notin $localVariables -and
            -not [SpecialVariables]::IsSpecialVariable($PSItem)
        }

        $parameters = GetInferredParameters $variableExpressions
        $destination = ValidateDestination
        if ($destination -eq 'ExternalFile') {
            [string] $targetFile = ValidateDestinationFile
        }

        $functionText = NewFunctionDefinition

        $invocation = [System.Text.StringBuilder]::new($FunctionName)
        foreach ($parameter in $parameters.Values) {
            $null = $invocation.AppendFormat(' -{0} ${1}', $parameter.Item1, $parameter.Item2)
        }

        $invocation = $invocation | AddIndent -Amount $targetExtentIndent

        $targetExtent | Set-ScriptExtent -Text $invocation

        switch ($destination) {
            Inline       { ExportFunctionInline }
            ExternalFile { ExportFunctionExternalFile }
            default      { ExportFunctionBegin }
        }
    }
}
