Import-LocalizedData -BindingVariable Strings -FileName Strings -ErrorAction Ignore

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

"$PSScriptRoot\Classes", "$PSScriptRoot\Public", "$PSScriptRoot\Private" |
    Get-ChildItem -Filter '*.ps1' |
    ForEach-Object { . $PSItem.FullName }

# Export only the functions using PowerShell standard verb-noun naming.
# Be sure to list each exported functions in the FunctionsToExport field of the module manifest file.
# This improves performance of command discovery in PowerShell.
Export-ModuleMember -Function *-*
