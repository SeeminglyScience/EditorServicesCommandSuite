using namespace System.Reflection
using namespace System.Collections.ObjectModel
using namespace System.Management.Automation.Language

class TypeExpressionHelper {
    [type] $Type;

    hidden [bool] $encloseWithBrackets;
    hidden [bool] $needsProxy;

    TypeExpressionHelper ([type] $type) {
        $this.Type = $Type
    }
    static [string] Create ([type] $type) {
        return [TypeExpressionHelper]::Create($type, $true)
    }
    static [string] Create ([type] $type, [bool] $encloseWithBrackets) {
        $helper = [TypeExpressionHelper]::new($type)
        $helper.encloseWithBrackets = $encloseWithBrackets
        return $helper.Create()
    }
   [string] Create () {
        # Non public types can't be retrieved with a type literal expression and need to be retrieved
        # from their assembly directly. The easiest way is to get a type from the same assembly and
        # get the assembly from that. The goal here is to build it as short as possible, hopefully
        # retaining some semblance of readability.
        if (-not $this.Type.IsPublic -or $this.Type.GenericTypeArguments.IsPublic -contains $false) {
            $this.needsProxy = $true
            return $this.CreateProxy()
        }
        else {
            return $this.CreateLiteral()
        }
    }
    hidden [string] CreateProxy () {
        $builder = [System.Text.StringBuilder]::new('[')
        $assembly = $this.Type.Assembly

        # First check if there are any type accelerators in the same assembly.
        $choices = $this.GetAccelerators().GetEnumerator().Where{ $PSItem.Value.Assembly -eq $assembly }.Key

        if (-not $choices) {
            # Then as a last resort pull every type from the assembly. This takes a extra second or
            # two the first time.
            $choices = $assembly.GetTypes().ToString
        }

        $builder.
            Append(($choices | Sort-Object Length)[0]).
            Append('].Assembly.GetType(''')

        if ($this.Type.GenericTypeArguments) {
            # Using the GetType method on the full name doesn't work for every type/combination, so
            # we use the MakeGenericType method.
            return $builder.AppendFormat('{0}.{1}'').MakeGenericType(', $this.Type.Namespace, $this.Type.Name).
                Append($this.GetGenericArguments()).
                Append(')').
                ToString()
        }
        else {
            return $builder.
                AppendFormat('{0}'')', $this.Type.ToString()).
                ToString()
        }
    }
    hidden [string] CreateLiteral () {
        $builder = [System.Text.StringBuilder]::new()
        # If we are building the type name as a generic type argument in a type literal we don't want
        # to enclose it with brackets.
        if ($this.encloseWithBrackets) { $builder.Append('[') }

        if ($this.Type.GenericTypeArguments) {
            $builder.
                AppendFormat('{0}.{1}', $this.Type.Namespace, $this.Type.Name).
                Append('[').
                Append($this.GetGenericArguments()).
                Append(']')
        }
        else {
            $name = $this.GetAccelerators().
                GetEnumerator().
                Where{ $PSItem.Value -eq $this.Type }.
                Key |
                Sort-Object Length

            if (-not $name) { $name = ($this.Type.Name -as [type]).Name }
            if (-not $name) { $name = $this.Type.ToString() }

            if ($name.Count -gt 1) { $name = $name[0] }

            $builder.Append($name)
        }

        if ($this.encloseWithBrackets) { $builder.Append(']') }

        return $builder.ToString()
    }
    hidden [string] GetGenericArguments () {
        $typeArguments = $this.Type.GenericTypeArguments

        $enclose = $false
        if ($this.needsProxy) { $enclose = $true }

        return $typeArguments.ForEach{
            [TypeExpressionHelper]::Create($PSItem, $enclose)
        } -join ', '
    }
    hidden [System.Collections.Generic.Dictionary[string, type]] GetAccelerators () {
       return [ref].Assembly.GetType('System.Management.Automation.TypeAccelerators')::Get
    }
}

class ExtendedMemberExpressionAst : MemberExpressionAst {
    [type] $InferredType;
    [MemberInfo] $InferredMember;
    [BindingFlags] $BindingFlags;
    [ReadOnlyCollection[ExpressionAst]] $Arguments;

    ExtendedMemberExpressionAst ([IScriptExtent] $extent,
                                 [ExpressionAst] $expression,
                                 [CommandElementAst] $member,
                                 [bool] $static,
                                 [ReadOnlyCollection[ExpressionAst]] $arguments) :
                                 base($extent, $expression, $member, $static) {

        try {
            $this.Arguments      = $arguments
            $this.InferredMember = GetInferredMember -Ast $this
            $this.InferredType   = ($this.InferredMember.ReturnType,
                                    $this.InferredMember.PropertyType,
                                    $this.InferredMember.FieldType).
                                    Where({ $PSItem }, 'First')[0]

            $this.BindingFlags   = $this.InferredMember.GetType().
                GetProperty('BindingFlags', [BindingFlags]'Instance, NonPublic').
                GetValue($this.InferredMember)
        } catch {
            $this.InferredType = [object]
        }
    }
    static [ExtendedMemberExpressionAst] op_Implicit ([MemberExpressionAst] $ast) {

        $expression = $ast.Expression.Copy()
        if ($expression -is [MemberExpressionAst]) {
            $expression = [ExtendedMemberExpressionAst]$expression
        }
        $newAst = [ExtendedMemberExpressionAst]::new(
            $ast.Extent,
            $expression,
            $ast.Member.Copy(),
            $ast.Static,
            $ast.Arguments
        )

        if ($ast.Parent) {
            $ast.Parent.GetType().
                GetMethod('SetParent', [BindingFlags]'Instance, NonPublic').
                Invoke($ast.Parent, $newAst)
        }

        return $newAst
    }
}
