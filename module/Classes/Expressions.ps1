using namespace System.Text
using namespace Microsoft.PowerShell

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
        $builder = [StringBuilder]::new('[')
        $assembly = $this.Type.Assembly

        # First check if there are any type accelerators in the same assembly.
        $choices = [ref].
            Assembly.
            GetType('System.Management.Automation.TypeAccelerators')::
            Get.
            GetEnumerator().
            Where{ $PSItem.Value.Assembly -eq $assembly }.Key

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
        $builder = [StringBuilder]::new()
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
            $name = [string]::Empty
            # Try to resolve the type name with a code method that won't take this script's
            # using statements into account. This also handles type accelerators for us.
            $resolvableName = [ToStringCodeMethods]::Type($this.Type)
            if ($resolvableName -as [type]) {
                $name = $resolvableName
            }

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
}
