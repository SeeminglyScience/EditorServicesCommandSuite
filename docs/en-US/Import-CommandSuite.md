---
external help file: EditorServicesCommandSuite-help.xml
online version: https://github.com/SeeminglyScience/EditorServicesCommandSuite/blob/master/docs/en-US/Import-CommandSuite.md
schema: 2.0.0
---

# Import-CommandSuite

## SYNOPSIS

Initialize the EditorServicesCommandSuite module.

## SYNTAX

```powershell
Import-CommandSuite
```

## DESCRIPTION

The Import-CommandSuite function imports the EditorServicesCommandSuite module and initalizes internal processes like setting up event handlers. You can import the module directly without using this function, but it isn't supported and may cause unexpected behavior. This function can be invoked after the module is loaded in case of accidental or auto loading.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
Import-CommandSuite
```

Imports EditorServicesCommandSuite functions, editor commands, and event handlers.

## PARAMETERS

## INPUTS

### None

This function does not accept input from the pipeline.

## OUTPUTS

### None

This function does not output to the pipeline.

## NOTES

## RELATED LINKS
