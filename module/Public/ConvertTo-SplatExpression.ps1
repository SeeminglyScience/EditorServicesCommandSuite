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
        $Ast
    )
    end {
        $Ast = GetAncestorOrThrow $Ast -AstTypeName CommandAst -ErrorContext $PSCmdlet

        $commandName, $elements = $Ast.CommandElements.Where({ $true }, 'Split', 1)

        $splat          = @{}
        $retainedArgs   = [List[Ast]]::new()
        $elementsExtent = $elements.Extent | Join-ScriptExtent
        $elements       = [Queue[Ast]]::new($elements -as [Ast[]])

        # Start building the hash table of named parameters and values
        while ($elements.Count -and ($current = $elements.Dequeue())) {
            if ($current -isnot [CommandParameterAst]) {
                # We don't try to figure out positional arguments, so we keep them in final CommandAst
                $retainedArgs.Add($current)
                continue
            }
            # The while is to loop through consecutive switch parameters.
            while ($current -is [CommandParameterAst]) {
                $lastParam = $current
                if (-not $elements.Count) {
                    $splat.$lastParam = '$true'
                    break
                }

                $current = $elements.Dequeue()

                if ($current -is [CommandParameterAst]) {
                    $splat.$lastParam = '$true'
                } else {
                    $splat.$lastParam = $current
                }
            }
        }

        # Remove the hypen, change to camelCase and add 'Splat'
        $variableName = [regex]::Replace(
            ($commandName.Extent.Text -replace '-'),
            '^[A-Z]',
            { $args[0].Value.ToLower() }) +
            'Splat'

        $sb = [System.Text.StringBuilder]::
            new('${0}' -f $variableName).
            AppendLine(' = @{')

        # All StringBuilder methods return itself so it can be chained.  We null the whole scriptblock
        # here so unchained method calls don't add to our output.
        $null = & {
            foreach($pair in $splat.GetEnumerator()) {
                $sb.Append('    ').
                    Append($pair.Key.ParameterName).
                    Append(' = ')

                $isStringExpression = $pair.Value -is [StringConstantExpressionAst] -or
                                      $pair.Value -is [ExpandableStringExpressionAst]

                if ($isStringExpression) {
                    # If kind isn't BareWord then it's already enclosed in quotes.
                    if ('BareWord' -ne $pair.Value.StringConstantType) {
                        $sb.AppendLine($pair.Value.Extent.Text)
                    } else {
                        $enclosure = "'"
                        if ($pair.Value.NestedExpressions) {
                            $enclosure = '"'
                        }

                        $sb.AppendFormat('{0}{1}{0}', @($enclosure, $pair.Value.Value)).
                            AppendLine()
                    }
                # When we handle switch parameters we don't create an AST.
                } elseif ($pair.Value -isnot [Ast]) {
                    $sb.AppendLine($pair.Value)
                } else {
                    $sb.AppendLine($pair.Value.Extent.Text)
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
