---
external help file: EditorServicesCommandSuite-help.xml
online version: https://github.com/SeeminglyScience/EditorServicesCommandSuite/blob/master/docs/en-US/Expand-Expression.md
schema: 2.0.0
---

# Expand-Expression

## SYNOPSIS

Replaces an extent with the return value of it's text as an expression.

## SYNTAX

```powershell
Expand-Expression [[-InputObject] <IScriptExtent[]>] [<CommonParameters>]
```

## DESCRIPTION

The Expand-Expression function replaces text at a specified range with it's output in PowerShell. As an editor command it will expand output of selected text.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
$psEditor.GetEditorContext().SelectedRange | ConvertTo-ScriptExtent | Expand-Expression
```

Invokes the currently selected text and replaces it with it's output.
This is also the default.

## PARAMETERS

### -InputObject

Specifies the extent to invoke.

```yaml
Type: IScriptExtent[]
Parameter Sets: (All)
Aliases: Extent

Required: False
Position: 1
Default value: ($psEditor.GetEditorContext().SelectedRange | ConvertTo-ScriptExtent)
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.Management.Automation.Language.IScriptExtent

You can pass extents to invoke from the pipeline.

## OUTPUTS

### None

## NOTES

## RELATED LINKS

