# These classes are the renderers that provide custom format functions for use in StringTemplates.
using namespace Antlr4.StringTemplate

enum IndentKind {
    Space;
    Tab;
}

# Base class for custom format functions in StringTemplates.
#
# TODO: Add indentation frames similar to in CustomControlBuilder to avoid having to fix indentation
#       post template invocation.
class StringExpressionRenderer : StringRenderer {
    [IndentKind] $IndentKind = [IndentKind]::Space;

    [string] ToString([object] $o, [string] $formatString, [cultureinfo] $culture) {
        if ($formatString -and $this.psobject.Methods.Match($formatString)) {
            return $this.$formatString($o)
        } elseif ($formatString) {
            return ([StringRenderer]$this).ToString($o, $formatString, $culture)
        }
        return $o -as [string]
    }

    [string] ToCamelCase([string] $o) {
        if (-not $o) { return $o }

        if ($o.Length -gt 1) {
            return '{0}{1}' -f $o.Substring(0, 1).ToLower(), $o.SubString(1, $o.Length - 1)
        } else {
            return $o.ToLower()
        }
    }

    # Allows inserting multiple tabs with one template call.
    [string] Tab([string] $o) {
        return $this.GetIndent() * [int]$o
    }

    # Currently always returns spaces.
    # TODO: Rig this up as a setting, or preferably get it from PSES.
    hidden [string] GetIndent() {
        if ($this.IndentKind -eq [IndentKind]::Space) {
            return '    '
        } else {
            return "`t"
        }
    }
}
# Format functions specific to Expand-MemberExpression.
class MemberExpressionRenderer : StringExpressionRenderer {
    # Transform member name for use as a variable name.
    [string] TransformMemberName([string] $o) {
        return $this.ToCamelCase(($o -replace '^\.ctor', 'new'))
    }
}

# Format function to allow using [TypeExpressionHelper] in StringTemplates.
class TypeRenderer : StringRenderer {
    [string] ToString([object] $o, [string] $formatString, [cultureinfo] $culture) {
        if ($o -is [type]) {
            return [TypeExpressionHelper]::Create($o)
        }
        return ([StringRenderer]$this).ToString($o, $formatString, $culture)
    }
}
