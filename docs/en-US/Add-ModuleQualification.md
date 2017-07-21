---
external help file: EditorServicesCommandSuite-help.xml
online version: https://github.com/SeeminglyScience/EditorServicesCommandSuite/docs/en-US/Add-ModuleQualification.md
schema: 2.0.0
---

# Add-ModuleQualification

## SYNOPSIS

Add a commands module name to it's invocation expression.

## SYNTAX

```powershell
Add-ModuleQualification [[-Ast] <Ast>]
```

## DESCRIPTION

The Add-ModuleQualification function retrieves the module a command belongs to and prepends the module name to the expression.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
# Place your cursor within this command and invoke the Add-ModuleQualification command.
Get-Command

# It becomes:
Microsoft.PowerShell.Core\Get-Command
```

Adds module qualification to a command expression.

## PARAMETERS

### -Ast

Specifies the CommandAst or AST within the CommandAst to add module qualification.

```yaml
Type: Ast
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
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
