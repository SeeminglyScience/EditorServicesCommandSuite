$moduleName = 'EditorServicesCommandSuite'
$manifestPath = "$PSScriptRoot\..\Release\$moduleName\*\$moduleName.psd1"

# These are just default tests from the boilerplate.  Testing reliably in an editor still needs
# to be worked out.

Describe 'module manifest values' {
    It 'can retrieve manfiest data' {
        $script:manifest = Test-ModuleManifest $manifestPath
    }
    It 'has the correct name' {
        $script:manifest.Name | Should Be $moduleName
    }
    It 'has the correct guid' {
        $script:manifest.Guid | Should Be '97607afd-d9bd-4a2e-a9f9-70fe1a0a9e4c'
    }
}

