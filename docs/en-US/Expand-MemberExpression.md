---
external help file: EditorServicesCommandSuite-help.xml
online version: https://github.com/SeeminglyScience/EditorServicesCommandSuite/blob/master/docs/en-US/Expand-MemberExpression.md
schema: 2.0.0
---

# Expand-MemberExpression

## SYNOPSIS

Builds an expression for accessing or invoking a member through reflection.

## SYNTAX

```powershell
Expand-MemberExpression [[-Ast] <Ast>] [-TemplateName <String>] [-NoParameterNameComments] [<CommonParameters>]
```

## DESCRIPTION

The Expand-MemberExpression function creates an expression for the closest MemberExpressionAst to the cursor in the current editor context. This is mainly to assist with creating expressions to access private members of .NET classes through reflection.

The expression is created using string templates. There are templates for several ways of accessing members including InvokeMember, GetProperty/GetValue, and a more verbose GetMethod/Invoke. If using the GetMethod/Invoke template it will automatically build type expressions for the "types" argument including nonpublic and generic types. If a template is not specified, this function will attempt to determine the most fitting template. If you have issues invoking a method with the default, try the VerboseInvokeMethod template. This function currently works on member expressions attached to the following:

1. Type literal expressions (including invalid expressions with non public types)

2. Variable expressions where the variable exists within a currently existing scope.

3. Any other scenario where standard completion works.

4. Any number of nested member expressions where one of the above is true at some point in the chain.


Additionally chains may break if a member returns a type that is too generic like System.Object or a vague interface.

## EXAMPLES

### -------------------------- EXAMPLE 1 --------------------------

```powershell
Expand-MemberExpression
```

Expands the member expression closest to the cursor in the current editor context using an automatically determined template.

### -------------------------- EXAMPLE 2 --------------------------

```powershell
Expand-MemberExpression -Template VerboseInvokeMethod
```

Expands the member expression closest to the cursor in the current editor context using the VerboseInvokeMethod template.

## PARAMETERS

### -Ast

Specifies the member expression ast (or child of) to expand.

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

### -TemplateName

A template is automatically chosen based on member type and visibility.  You can use this parameter to force the use of a specific template.

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

### -NoParameterNameComments

By default expanded methods will have a comment with the parameter name on each line. (e.g. `<# paramName: #> $paramName,`) If you specify this parameter it will be omitted.

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

### CommonParameters

This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None

## OUTPUTS

### None

## NOTES

## RELATED LINKS

