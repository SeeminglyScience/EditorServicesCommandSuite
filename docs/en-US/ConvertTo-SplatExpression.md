---
external help file: EditorServicesCommandSuite-help.xml
online version: https://github.com/SeeminglyScience/EditorServicesCommandSuite/docs/en-US/ConvertTo-SplatExpression.md
schema: 2.0.0
---

# ConvertTo-SplatExpression

## SYNOPSIS

Convert a command expression to use splatting.

## SYNTAX

```powershell
ConvertTo-SplatExpression [[-Ast] <Ast>]
```

## DESCRIPTION

The ConvertTo-SplatExpression function transforms a CommandAst to use a splat expression instead
of inline parameters.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
# Place your cursor inside this command and run this function:
Get-ChildItem .\Path -Force -File -Filter *.txt -Exclude *$myExclude* -Recurse

# It becomes:
$getChildItemSplat = @{
    File = $true
    Filter = '*.txt'
    Exclude = "*$myExclude*"
    Force = $true
    Recurse = $true
}
Get-ChildItem @getChildItemSplat .\Path
```

Uses this function as an editor command to expand a long command into a splat expression.

## PARAMETERS

### -Ast

Specifies an Ast that is, or is within the CommandAst to be converted.

```yaml
Type: Ast
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: (Find-Ast -AtCursor)
Accept pipeline input: False
Accept wildcard characters: False
```

## INPUTS

## OUTPUTS

## NOTES

## RELATED LINKS
