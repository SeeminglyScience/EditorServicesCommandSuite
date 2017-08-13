---
external help file: EditorServicesCommandSuite-help.xml
online version: https://github.com/SeeminglyScience/EditorServicesCommandSuite/blob/master/docs/en-US/Add-PinvokeMethod.md
schema: 2.0.0
---

# Add-PinvokeMethod

## SYNOPSIS

Find and insert a PInvoke function signature into the current file.

## SYNTAX

```powershell
Add-PinvokeMethod [[-Function] <String>] [[-Module] <String>]
```

## DESCRIPTION

The Add-PinvokeMethod function searches pinvoke.net for the requested function name and provides a list of matches to select from.  Once selected, this function will get the signature and create a expression that uses the Add-Type cmdlet to create a type with the PInvoke method.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
Add-PinvokeMethod -Function SetConsoleTitle -Module Kernel32

# Inserts the following into the file currently open in the editor.

# Source: http://pinvoke.net/jump.aspx/kernel32.setconsoletitle
Add-Type -Namespace PinvokeMethods -Name Kernel -MemberDefinition '
[DllImport("kernel32.dll")]
public static extern bool SetConsoleTitle(string lpConsoleTitle);'
```

Adds code to use the SetConsoleTitle function from the kernel32 DLL.

## PARAMETERS

### -Function

Specifies the function name to search for. If omitted, a prompt will be displayed within the editor.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -Module

Specifies the module or dll the function resides in. If omitted, and multiple matching functions exist, a choice prompt will be displayed within the editor.

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

This function does not accept input from the pipeline.

## OUTPUTS

### None

This function does not output to the pipeline.

## NOTES

## RELATED LINKS

[pinvoke.net](http://pinvoke.net/)
