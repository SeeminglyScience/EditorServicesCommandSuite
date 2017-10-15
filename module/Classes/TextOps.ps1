
enum CaseType {
    Pascal;
    Camel;
}

class TextOps {
    static [string] TransformCase([psobject] $text, [CaseType] $type) {
        $methodName = 'ToUpperInvariant'
        if ($type -eq [CaseType]::Camel) {
            $methodName = 'ToLowerInvariant'
        }

        $asString = $text -as [string]
        if ([string]::IsNullOrWhiteSpace($asString)) {
            return [string]::Empty
        }

        if ($asString.Length -le 2) {
            return $asString.$methodName()
        }

        return $asString.Substring(0, 1).$methodName() +
                ($asString[1..$asString.Length] -join '')
    }

    static [string] ToCamelCase([psobject] $text) {
        return [TextOps]::TransformCase($text, [CaseType]::Camel)
    }

    static [string] ToPascalCase([psobject] $text) {
        return [TextOps]::TransformCase($text, [CaseType]::Pascal)
    }
}
