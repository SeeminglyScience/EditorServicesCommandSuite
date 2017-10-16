using namespace System.Management.Automation
using namespace System.Reflection
using namespace System.Text

class MemberExpressionGeneration {
    # Work around for type resolution issues.
    hidden static [string] $Indent = ('TextOps' -as [type])::Indent

    [MemberInfo] $Member;
    [psobject] $Target;
    [bool] $BeVerbose;

    hidden [bool] $WasVerbose;
    hidden [StringBuilder] $Builder;
    hidden [string] $Flags;
    hidden [bool] $IsMethod;
    hidden [bool] $IsConstructor;

    hidden MemberExpressionGeneration([MemberInfo] $member, [psobject] $target) {
        $this.Target = $target
        $this.Member = $member
        $this.IsMethod = $this.Member -is [MethodBase]
        $this.IsConstructor = $this.Member -is [ConstructorInfo]
        $this.Flags = "[System.Reflection.BindingFlags]'{0}'" -f (
            [MemberExpressionGeneration]::GetBindingFlags(
                $this.Member))

        $this.Builder = [StringBuilder]::new()
        $this.Builder.psobject.Methods.Add(
            [PSCodeMethod]::new(
                'AppendInvocationArgs',
                [MemberExpressionGeneration].GetMethod('InvocationArgsCodeMethod')))

        $this.Builder.psobject.Methods.Add(
            [PSCodeMethod]::new(
                'AppendParameterName',
                [MemberExpressionGeneration].GetMethod('CommentCodeMethod')))

        $this.Builder.psobject.Methods.Add(
            [PSCodeMethod]::new(
                'AppendIndent',
                [MemberExpressionGeneration].GetMethod('IndentCodeMethod')))
    }

    static [string] GetReflectionExpression([psobject] $instance, [MemberInfo] $member) {
        return [MemberExpressionGeneration]::GetReflectionExpression(
            $instance,
            $member,
            $false)
    }

    static [string] GetReflectionExpression(
        [psobject] $instance,
        [MemberInfo] $member,
        [bool] $beVerbose)
    {
        $helper = [MemberExpressionGeneration]::new($member, $instance)
        $helper.BeVerbose = $beVerbose
        return $helper.GetExpression()
    }

    static [string[]] GetInvocationArgs([ParameterInfo[]] $parameters) {
        return [MemberExpressionGeneration]::GetInvocationArgs($parameters, $true)
    }

    static [string[]] GetInvocationArgs([ParameterInfo[]] $parameters, [bool] $includeNameComments) {
        $result = foreach ($parameter in $parameters) {
            $comment = ''
            if ($includeNameComments) {
                $comment = '<# {0}: #> ' -f $parameter.Name
            }

            '{0}${1}' -f $comment, $parameter.Name
        }

        return $result
    }

    hidden static [StringBuilder] InvocationArgsCodeMethod(
        [psobject] $instance,
        [int] $indent,
        [MemberInfo] $member)
    {
        $parameters = $member.GetParameters()
        if (-not $parameters.Count) {
            return $instance
        }

        $indentText = (@([MemberExpressionGeneration]::Indent) * $indent) -join ''

        if ($parameters.Count) {
            $instance.AppendLine().Append($indentText)
        }

        return $instance.Append(
            [MemberExpressionGeneration]::GetInvocationArgs($parameters) -join (
                ',' + [Environment]::NewLine + $indentText))
    }

    static hidden [StringBuilder] CommentCodeMethod([psobject] $instance, [string] $parameterName) {
        return $instance.AppendFormat('<# {0}: #> ', $parameterName)
    }

    static hidden [StringBuilder] IndentCodeMethod([psobject] $instance) {
        return $instance.Append([MemberExpressionGeneration]::Indent)
    }

    static hidden [BindingFlags] GetBindingFlags([MemberInfo] $member) {
        $scope = [BindingFlags]::Instance
        if ([MemberExpressionGeneration]::IsStatic($member)) {
            $scope = [BindingFlags]::Static
        }

        return [BindingFlags]::NonPublic -bor $scope
    }

    static hidden [bool] IsStatic([MemberInfo] $member) {
        if ($member -is [PropertyInfo]) {
            return $member.GetMethod.IsStatic
        }

        return $member.IsStatic
    }

    [string] GetExpression() {
        $this.WriteBridge()
        $this.WriteGetArgs()
        $this.WriteInvocationExpression()

        return $this.Builder.ToString()
    }

    hidden [void] WriteBridge() {
        $this.Builder.
            Append($this.GetSourceExpression()).
            AppendLine('.').
            AppendIndent().
            Append('Get').
            Append($this.Member.MemberType).
            Append('(')
    }

    hidden [void] WriteGetArgs() {
        $canBeVerbose = -not ([MemberTypes]'Property, Field').HasFlag($this.Member.MemberType)
        if ($this.BeVerbose -and $canBeVerbose ) {
            $this.WriteComplexGetArgs()
            return
        }

        $this.WriteBasicGetArgs()
    }

    hidden [void] WriteInvocationExpression() {
        $methodName = 'GetValue'
        if ($this.IsMethod) {
            $methodName = 'Invoke'
        }

        $this.Builder.AppendLine().AppendIndent().
            Append($methodName).Append('(')

        if (-not $this.IsConstructor) {
            $this.Builder.Append($this.GetTargetExpression())

            if (-not $this.IsMethod) {
                $this.Builder.Append(')')
                return
            }

            $this.Builder.Append(', ')
        }

        $this.Builder.
            Append('@(').
            AppendInvocationArgs(2, $this.Member).
            Append('))')
    }

    hidden [string] GetSourceExpression() {
        if ($this.IsConstructor) {
            if ($this.Target -isnot [type]) {
                $this.Target = $this.Member.ReflectedType
            }

            return [TypeExpressionHelper]::Create($this.Target)
        }

        if ($this.Target -is [type]) {
            return [TypeExpressionHelper]::Create($this.Target)
        }

        return $this.Target.ToString() + '.GetType()'
    }

    hidden [string] GetTargetExpression() {
        if ($this.IsConstructor) {
            return [string]::Empty
        }

        if ([MemberExpressionGeneration]::IsStatic($this.Member)) {
            return '$null'
        }

        if ($this.Target -is [type]) {
            return '$instance'
        }

        return $this.Target.ToString()
    }

    hidden [void] WriteBasicGetArgs() {
        $this.
            Builder.
            AppendFormat("'{0}', ", $this.Member.Name).
            Append($this.Flags).
            Append(").")
    }

    hidden [void] WriteComplexGetArgs() {
        if ($this.Member -isnot [ConstructorInfo]) {
            $this.Builder.AppendLine().AppendIndent().AppendIndent().
                AppendParameterName('name').
                AppendFormat("'{0}',", $this.Member.Name)
        }

        $this.Builder.AppendLine().AppendIndent().AppendIndent().
            AppendParameterName('bindingAttr').
            Append($this.Flags).
            Append(',')

        $this.Builder.AppendLine().AppendIndent().AppendIndent().
            AppendParameterName('binder').
            Append('$null,')

        $types = foreach($parameter in $this.Member.GetParameters()) {
            [TypeExpressionHelper]::Create($parameter.ParameterType)
        }
        $this.Builder.AppendLine().AppendIndent().AppendIndent().
            AppendParameterName('types').
            AppendFormat('@({0}),', $types -join ', ')

        $modifiers = [int]($this.Member.GetParameters().Count)
        if (-not $modifiers) {
            $modifiers = '@()'
        }

        $this.Builder.AppendLine().AppendIndent().AppendIndent().
            AppendParameterName('modifiers').
            Append($modifiers).
            Append(').')
    }
}
