using namespace System.Management.Automation
using namespace System.Management.Automation.Language

class SpecialVariables {
    static [System.Lazy[string[]]] $SpecialVariables = [Lazy[string[]]]::new(
        [Func[string[]]]{
            # Nothing public exists to get this unfortunately.
            return [ref].
                Assembly.
                GetType('System.Management.Automation.SpecialVariables').
                DeclaredFields.
                Where{ $PSItem.FieldType -eq [string] }.
                ForEach{ $PSItem.GetValue($null) }
        });

    static [bool] IsSpecialVariable([VariableExpressionAst] $variable) {
        return [SpecialVariables]::IsSpecialVariable($variable.VariablePath)
    }

    static [bool] IsSpecialVariable([VariablePath] $variable) {
        return [SpecialVariables]::IsSpecialVariable($variable.UserPath)
    }

    static [bool] IsSpecialVariable([string] $variable) {
        if ([string]::IsNullOrEmpty($variable)) {
            return $false
        }

        return $variable -in [SpecialVariables]::SpecialVariables.Value -or $variable -eq 'psEditor'
    }
}
