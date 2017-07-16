---
external help file: EditorServicesCommandSuite-help.xml
online version: https://github.com/SeeminglyScience/EditorServicesCommandSuite/blob/master/docs/en-US/Expand-TypeImplementation.md
schema: 2.0.0
---

# Expand-TypeImplementation

## SYNOPSIS

Expand the closest type expression into a implementation using PowerShell classes.

## SYNTAX

```powershell
Expand-TypeImplementation [[-Type] <Type[]>]
```

## DESCRIPTION

The Expand-TypeImplementation function generates code to implement a class. You can specify a type to implement, or place your cursor close to a type expression and invoke this as an editor command.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
$type = [System.Management.Automation.IArgumentCompleter]
Expand-TypeImplementation -Type $type

# Adds the following code to the current file.

class NewIEqualityComparer : System.Collections.IEqualityComparer {
    [bool] Equals ([Object] $x, [Object] $y) {
        throw [NotImplementedException]::new()
    }

    [int] GetHashCode ([Object] $obj) {
        throw [NotImplementedException]::new()
    }
}
```

## PARAMETERS

### -Type

Specifies the type to implement.

```yaml
Type: Type[]
Parameter Sets: (All)
Aliases:

Required: False
Position: 1
Default value: None
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

## INPUTS

### Type

You can pass types to implement to this function.

## OUTPUTS

### None

## NOTES

## RELATED LINKS
