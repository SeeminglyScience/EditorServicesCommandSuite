---
external help file: EditorServicesCommandSuite-help.xml
online version: https://github.com/SeeminglyScience/EditorServicesCommandSuite/docs/en-US/New-ESCSSettingsFile.md
schema: 2.0.0
---

# New-ESCSSettingsFile

## SYNOPSIS

Create a new settings file for the current workspace.

## SYNTAX

```powershell
New-ESCSSettingsFile [[-Path] <String>] [-Force]
```

## DESCRIPTION

The New-ESCSSettingsFile function creates a settings file in the current workspace. This file contains settings used by this module for determining where to find specific files.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
New-ESCSSettingsFile
```

Creates the file ESCSSettings.psd1 in the base of the current workspace with default values.

## PARAMETERS

### -Path

Specifies the path to save the settings file to. If this parameter is not specified a settings file will be created in the base of the current workspace.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: $psEditor.Workspace.Path
Accept pipeline input: False
Accept wildcard characters: False
```

### -Force

If specified indicates that an existing settings file should be overridden without prompting.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

## INPUTS

### None

This function does not accept value from the pipeline.

## OUTPUTS

### None

This function does not output to the pipeline.

## NOTES

## RELATED LINKS
