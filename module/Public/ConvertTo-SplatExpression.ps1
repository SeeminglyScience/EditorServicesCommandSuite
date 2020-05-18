using namespace Microsoft.PowerShell.EditorServices.Extensions
using namespace System.Collections.Generic
using namespace System.Management.Automation.Language

function ConvertTo-SplatExpression {
    <#
    .EXTERNALHELP EditorServicesCommandSuite-help.xml
    #>
    [CmdletBinding()]
    [EditorCommand(DisplayName='Convert Command to Splat Expression')]
    param(
        [System.Management.Automation.Language.Ast]
        $Ast,

        [String]
        $VariableName,

        [ValidateSet('camelCase', 'PascalCase', 'Unmodified')]
        [String]
        $VariableCase = 'camelCase'
    )
    begin {
        function ConvertFromExpressionAst($expression) {
            $isStringExpression = $expression -is [StringConstantExpressionAst] -or
                                  $expression -is [ExpandableStringExpressionAst]

            if ($isStringExpression) {
                # If kind isn't BareWord then it's already enclosed in quotes.
                if ('BareWord' -ne $expression.StringConstantType) {
                    return $expression.Extent.Text
                }
                $enclosure = "'"
                if ($expression.NestedExpressions) {
                    $enclosure = '"'
                }

                return '{0}{1}{0}' -f $enclosure, $expression.Value
            }
            # When we handle switch parameters we don't create an AST.
            if ($pair.Value -isnot [Ast]) {
                return $expression
            }

            return $expression.Extent.Text
        }
    }
    end {
        $Ast = GetAncestorOrThrow $Ast -AstTypeName CommandAst -ErrorContext $PSCmdlet

        $commandName, $elements = $Ast.CommandElements.Where({ $true }, 'Split', 1)

        $splat           = @{}
        $retainedArgs    = [List[Ast]]::new()
        $elementsExtent  = $elements.Extent | Join-ScriptExtent
        $boundParameters = [StaticParameterBinder]::BindCommand($Ast).BoundParameters

        # Start building the hash table of named parameters and values
        foreach ($parameter in $boundParameters.GetEnumerator()) {
            # If the command isn't loaded positional parameters come through as their numeric position.
            if ($parameter.Key -match '\d+' -and -not $parameter.Value.Parameter) {
                $retainedArgs.Add($parameter.Value.Value)
                continue
            }
            # The "Value" property for switches is the parameter AST (e.g. -Force) so we need to
            # manually build the expression.
            if ($parameter.Value.ConstantValue -is [bool]) {
                $splat.($parameter.Key) = '${0}' -f $parameter.Value.ConstantValue.ToString().ToLower()
                continue
            }
            $splat.($parameter.Key) = $parameter.Value.Value
        }

        if (-not $VariableName) {
            switch ($VariableCase) {
                'camelCase' {
                    # Remove the hyphen, change to pascalCase, and add 'Splat
                    $variableName = [regex]::Replace(
                        ($commandName.Extent.Text -replace '-'),
                        '^[A-Z]',
                        { $args[0].Value.ToLower() }) +
                        'Splat'
                }
                'PascalCase' {
                    # Remove the hyphen, change to PascalCase, and add 'Splat'
                    $variableName = [regex]::Replace(
                        ($commandName.Extent.Text -replace '-'),
                        '^[A-Z]',
                        { $args[0].Value.ToUpper() }) +
                        'Splat'
                }
                'Unmodified' {
                    # Remove the hyphen, don't change case, and add 'Splat'
                    $variableName = [regex]::Replace(
                        ($commandName.Extent.Text -replace '-'),
                        '^[A-Z]',
                        { $args[0].Value }) +
                        'Splat'
                }
            }
        }

        $sb = [System.Text.StringBuilder]::
            new('${0}' -f $variableName).
            AppendLine(' = @{')

        # All StringBuilder methods return itself so it can be chained.  We null the whole scriptblock
        # here so unchained method calls don't add to our output.
        $null = & {
            foreach($pair in $splat.GetEnumerator()) {
                $sb.Append('    ').
                    Append($pair.Key).
                    Append(' = ')
                if ($pair.Value -is [ArrayLiteralAst]) {
                    $sb.AppendLine($pair.Value.Elements.ForEach{
                        ConvertFromExpressionAst $PSItem
                    } -join ', ')
                } else {
                    $sb.AppendLine((ConvertFromExpressionAst $pair.Value))
                }
            }
            $sb.Append('}')
        }
        $splatText = $sb.ToString()

        # New CommandAst will be `Command @splatvar [PositionalArguments]`
        $newCommandParameters = '@' + $variableName
        if ($retainedArgs) {
            $newCommandParameters += ' ' + ($retainedArgs.Extent.Text -join ' ')
        }

        # Change the command expression first so we don't need to track it's position.
        $elementsExtent | Set-ScriptExtent -Text $newCommandParameters

        # Get the parent PipelineAst so we don't add the splat in the middle of a pipeline.
        $pipeline = $Ast | Find-Ast -Ancestor -First { $PSItem -is [PipelineAst] }

        # Prepend the existing indent.
        $lineText = ($psEditor.GetEditorContext().
            CurrentFile.
            Ast.
            Extent.
            Text -split '\r?\n')[$pipeline.Extent.StartLineNumber - 1]

        $lineIndent  = $lineText -match '^\s*' | ForEach-Object { $matches[0] }
        $splatText   = $lineIndent + (
                       $splatText -split '\r?\n' -join ([Environment]::NewLine + $lineIndent))

        # HACK: Temporary workaround until https://github.com/PowerShell/PowerShellEditorServices/pull/541
        #$splatTarget = ConvertTo-ScriptExtent -Line $pipeline.Extent.StartLineNumber
        $splatTarget = [Microsoft.PowerShell.EditorServices.FullScriptExtent]::new(
            $psEditor.GetEditorContext().CurrentFile,
            [Microsoft.PowerShell.EditorServices.BufferRange]::new(
                $pipeline.Extent.StartLineNumber,
                1,
                $pipeline.Extent.StartLineNumber,
                1))

        $splatTarget | Set-ScriptExtent -Text ($splatText + [Environment]::NewLine)
    }
}
