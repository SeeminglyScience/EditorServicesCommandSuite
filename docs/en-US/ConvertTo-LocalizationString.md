---
external help file: EditorServicesCommandSuite-help.xml
online version: https://github.com/SeeminglyScience/EditorServicesCommandSuite/docs/en-US/ConvertTo-LocalizationString.md
schema: 2.0.0
---

# ConvertTo-LocalizationString

## SYNOPSIS

Move a string expression to a localization resource file.

## SYNTAX

```powershell
ConvertTo-LocalizationString [[-Ast] <Ast>] [[-Name] <String>]
```

## DESCRIPTION

The ConvertTo-LocalizationString function will take the closest string expression and replace it with a variable that references a localization resource file.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
# Place your cursor inside the string and invoke this editor command:
Write-Verbose ('Writing to file at path "{0}".' -f $Path)

# It prompts you for a string name and becomes:
Write-Verbose ($Strings.YourStringName -f $Path)

# And adds this to your localization file:
YourStringName=Writing to file at path "{0}".
```

Uses this function as an editor command to replace a string expression with a reference to a localization file.

## PARAMETERS

### -Ast

Specifies the string expression to convert.

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

### -Name

Specifies the name to give the string in the localization file.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

## INPUTS

### None

This function does not accept input from the pipeline

## OUTPUTS

### None

This function does not output to the pipeline.

## NOTES

Current limitations:

- Only supports localization files that use ConvertFrom-StringData and a here-string
- Only supports using a single localization file

## RELATED LINKS
