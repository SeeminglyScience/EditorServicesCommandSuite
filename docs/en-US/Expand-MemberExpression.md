---
external help file: EditorServicesCommandSuite-help.xml
online version: https://github.com/SeeminglyScience/EditorServicesCommandSuite/blob/master/docs/en-US/Expand-MemberExpression.md
schema: 2.0.0
---

# Expand-MemberExpression

## SYNOPSIS

Expands a member expression into a more explicit form.

## SYNTAX

```powershell
Expand-MemberExpression [[-Ast] <Ast>] [-TemplateName <String>] [-NoParameterNameComments] [<CommonParameters>]
```

## DESCRIPTION

The Expand-MemberExpression function expands member expressions to a more explicit statement. This
function has two main purposes.

* Add parameter name comments (e.g. <# parameterName: #>) to method invocation arguments

* Invokable expressions that target non-public class members using reflection

As an editor command, this function will expand the AST closest to the current cursor location
if applicable.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
'[ConsoleKeyInfo]::new' | Out-File .\example1.ps1
psedit .\example1.ps1
$psEditor.GetEditorContext().SetSelection(1, 20, 1, 20)
Expand-MemberExpression
$psEditor.GetEditorContext().CurrentFile.GetText()

# [System.ConsoleKeyInfo]::new(
#    <# keyChar: #> $keyChar,
#    <# key: #> $key,
#    <# shift: #> $shift,
#    <# alt: #> $alt,
#    <# control: #> $control)

```

* Creates a new file with an unfinished member expression
* Opens it in the editor
* Sets the cursor within the member expression
* Invokes Expand-MemberExpression
* Returns the new expression

The new expression is expanded to include arguments and parameter name comments for every parameter.

### -------------------------- EXAMPLE 2 --------------------------

```powershell
'[sessionstatescope]::createfunction' | Out-File .\example2.ps1
psedit .\example2.ps1
$psEditor.GetEditorContext().SetSelection(1, 30, 1, 30)
Expand-MemberExpression
$psEditor.GetEditorContext().CurrentFile.GetText()

# $createFunction = [ref].Assembly.GetType('System.Management.Automation.SessionStateScope').
#     GetMethod('CreateFunction', [System.Reflection.BindingFlags]'Static, NonPublic').
#     Invoke($null, @(
#         <# name: #> $name,
#         <# function: #> $function,
#         <# originalFunction: #> $originalFunction,
#         <# options: #> $options,
#         <# context: #> $context,
#         <# helpFile: #> $helpFile))

```

* Creates a new file with an unfinished member expression
* Opens it in the editor
* Sets the cursor within the member expression
* Invokes Expand-MemberExpression
* Returns the new expression

The new expression generated will resolve the non-public type and invoke the non-public method.

### -------------------------- EXAMPLE 3 --------------------------

```powershell
'$ExecutionContext.SessionState.Internal.RemoveVariableAtScope' | Out-File .\example3.ps1
psedit .\example3.ps1
$psEditor.GetEditorContext().SetSelection(1, 60, 1, 60)
Expand-MemberExpression
# Manually select the last overload in the menu opened in the editor.
$psEditor.GetEditorContext().CurrentFile.GetText()

# $internal = $ExecutionContext.SessionState.GetType().
#     GetProperty('Internal', [System.Reflection.BindingFlags]'Instance, NonPublic').
#     GetValue($ExecutionContext.SessionState)
#
# $removeVariableAtScope = $internal.GetType().
#     GetMethod(
#         <# name: #> 'RemoveVariableAtScope',
#         <# bindingAttr: #> [System.Reflection.BindingFlags]'Instance, NonPublic',
#         <# binder: #> $null,
#         <# types: #> @([string], [string], [bool]),
#         <# modifiers: #> 3).
#     Invoke($internal, @(
#         <# name: #> $name,
#         <# scopeID: #> $scopeID,
#         <# force: #> $force))
```

* Creates a new file with an unfinished member expression
* Opens it in the editor
* Sets the cursor within the member expression
* Invokes Expand-MemberExpression
* Returns the new expression

This example shows that an expression will be generated for each non-public member in the chain. It
also demonstrates the ability to select an overload from a menu in the editor and the more alternate
syntax generated for harder to resolve methods.

## PARAMETERS

### -Ast

Specifies the AST of the member expression to expand.

```yaml
Type: Ast
Parameter Sets: (All)
Aliases:

Required: False
Position: 2
Default value: (Find-Ast -AtCursor)
Accept pipeline input: True (ByPropertyName, ByValue)
Accept wildcard characters: False
```

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### None

## NOTES

* When this function is building reflection statements, it will automatically choose the simpliest form
  of the Type.Get* methods that will resolve the target member.

* Member resolution is currently only possible in the following scenarios:
  * Type literal expressions, including invalid expressions with non public types like [localpipeline]
  * Variable expressions where the variable exists within a currently existing scope
  * Any other scenario where standard completion works
  * Any number of nested member expressions where one of the above is true at some point in the chain

* Member resolution may break in member chains if a member returns a type that is too generic like
  System.Object or IEnumerable

## RELATED LINKS
