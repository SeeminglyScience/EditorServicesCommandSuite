Import-LocalizedData -BindingVariable Strings -FileName Strings -ErrorAction Ignore

Import-Module $PSScriptRoot/EditorServicesCommandSuite.dll
Import-Module $PSScriptRoot/RefactorCmdlets/RefactorCmdlets.cdxml

if (-not $CommandSuite -or $CommandSuite -isnot [EditorServicesCommandSuite.Internal.CommandSuite]) {
    $IsMainRunspace = $true

    if ($null -ne $psEditor) {
        Add-Type -Path "$PSScriptRoot/EditorServicesCommandSuite.EditorServices.dll"
        try {
            $powerShellContext = [Microsoft.PowerShell.EditorServices.PowerShellContext]::new(
                [Microsoft.PowerShell.EditorServices.Utility.NullLogger]::new(),
                $false)
        } catch [System.Management.Automation.MethodException] {
            $powerShellContext = [Microsoft.PowerShell.EditorServices.PowerShellContext]::new(
                [Microsoft.PowerShell.EditorServices.Utility.NullLogger]::new())
        }

        $CommandSuite = [EditorServicesCommandSuite.EditorServices.Internal.CommandSuite]::GetCommandSuite(
            $psEditor,
            $ExecutionContext,
            $Host,
            $powerShellContext)
    } else {
        Add-Type -Path "$PSScriptRoot/EditorServicesCommandSuite.PSReadLine.dll"
        $CommandSuite = [EditorServicesCommandSuite.PSReadLine.Internal.CommandSuite]::GetCommandSuite(
            $ExecutionContext,
            $Host)
    }
} else {
    $IsMainRunspace = $false
}

$script:DEFAULT_SETTINGS = @{
    MainModuleDirectory        = '.\module'
    SourceManifestPath         = '.\module\*.psd1'
    MarkdownDocsPath           = '.\docs'
    StringLocalizationManifest = '.\module\en-US\Strings.psd1'
}

# PSST doesn't load Antlr until first use, and we need them loaded
# to create renderers.
if (-not ('Antlr4.StringTemplate.StringRenderer' -as [type])) {
    if (-not ($psstPath = (Get-Module PSStringTemplate).ModuleBase)) {
        # platyPS doesn't seem to be following RequiredModules, this should only ever run
        # while running platyPS.  Need to look into this more.
        $psstPath = (Get-Module PSStringTemplate -ListAvailable).ModuleBase
    }
    Add-Type -Path $psstPath\Antlr3.Runtime.dll
    Add-Type -Path $psstPath\Antlr4.StringTemplate.dll
}

# ~MONOLITH_INJECT_START~
# This section will be replaced with the contents of the files it calls during the build process.
# See tools/BuildMonolith.ps1 for more details.
"$PSScriptRoot\Classes", "$PSScriptRoot\Public", "$PSScriptRoot\Private" |
    Get-ChildItem -Filter '*.ps1' |
    ForEach-Object { . $PSItem.FullName }
# ~MONOLITH_INJECT_END~

if ($isMainRunspace) {
    $CommandSuite.ImportSessionRefactors($ExecutionContext.SessionState)
}

# Export only the functions using PowerShell standard verb-noun naming.
# Be sure to list each exported functions in the FunctionsToExport field of the module manifest file.
# This improves performance of command discovery in PowerShell.
Export-ModuleMember -Function *-* -Cmdlet *-*
