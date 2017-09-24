---
external help file: EditorServicesCommandSuite-help.xml
online version: https://github.com/SeeminglyScience/EditorServicesCommandSuite/blob/master/docs/en-US/ConvertTo-FunctionDefinition.md
schema: 2.0.0
---

# ConvertTo-FunctionDefinition

## SYNOPSIS

Create a new function from a selection or specified script extent object.

## SYNTAX

### __AllParameterSets (Default)

```powershell
ConvertTo-FunctionDefinition [-Extent <IScriptExtent>] [-FunctionName <String>]
```

### ExternalFile

```powershell
ConvertTo-FunctionDefinition [-Extent <IScriptExtent>] [-FunctionName <String>] [-DestinationPath <String>]
```

### BeginBlock

```powershell
ConvertTo-FunctionDefinition [-Extent <IScriptExtent>] [-FunctionName <String>] [-BeginBlock]
```

### Inline

```powershell
ConvertTo-FunctionDefinition [-Extent <IScriptExtent>] [-FunctionName <String>] [-Inline]
```

## DESCRIPTION

The ConvertTo-FunctionDefintion function takes a section of the current file and creates a function
definition from it. The generated function includes a parameter block with parameters for variables
that are not defined in the selection. In the place of the selected text will be the invocation of
the generated command including parameters.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
# Open a new untitled file
$psEditor.Workspace.NewFile()

# Insert some text into the file
$psEditor.GetEditorContext().CurrentFile.InsertText('
$myVar = "testing"
Get-ChildItem $myVar
')

# Select the Get-ChildItem line
$psEditor.GetEditorContext().SetSelection(3, 1, 4, 1)

# Convert it to a function
ConvertTo-FunctionDefinition -FunctionName GetMyDirectory -Inline

# Show the new contents of the file
$psEditor.GetEditorContext.CurrentFile.GetText()

# $myVar = "testing"
# function GetMyDirectory {
#     param([string] $MyVar)
#     end {
#         Get-ChildItem $MyVar
#     }
# }
#
# GetMyDirectory -MyVar $myVar

```

Creates a new untitled file in the editor, inserts demo text, and then converts a line to a inline function.

## PARAMETERS

### -Extent

The ScriptExtent to convert to a function. If not specified, the currently selected text in the editor will be used.

```yaml
Type: IScriptExtent
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -FunctionName

Specifies the name to give the generated function. If not specified, a input prompt will be displayed
in the editor.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -DestinationPath

Specifies a path relative to the file open in the editor to save the function to. You can specify an
existing or new file. If the file extension is omitted, the path is assumed to be a directory and a
file name is assumed to be the function name.

```yaml
Type: String
Parameter Sets: ExternalFile
Aliases:

Required: False
Position: Named
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -BeginBlock

If specified, the function will be saved to the Begin block of either the closest parent function
definition, or of the root script block if no function definitions exist.

If there is no Begin block available, one will be created. If a begin block must be created and no
named blocks exist yet, a separate End block will be created from the existing unnamed block.

```yaml
Type: SwitchParameter
Parameter Sets: BeginBlock
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -Inline

If specified, the function will be saved directly above the selection.

```yaml
Type: SwitchParameter
Parameter Sets: Inline
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

## INPUTS

### None

This function does not accept input from the pipeline.

## OUTPUTS

### None

This function does not return output to the pipeline.

## NOTES

## RELATED LINKS
