#requires -Version 5.1

<#
.SYNOPSIS
    Combine module files at build time to improve load time.

.DESCRIPTION
    The BuildMonolith script parses all function and class files in the source directory and combines
    then into a single .psm1 file, or a .psm1 and a Classes.ps1 file to avoid exporting classes.

    This process improves the load time of the module at the cost of increased complexity during the
    build process. I do not personally recommended replicating this process outside of this module
    unless you have a similar number of files.

.PARAMETER OutputPath
    Specifies the path to the release directory.

.PARAMETER ModuleName
    Specifies the name of the module being built. This will be used when naming the generated files.

.PARAMETER SourcePath
    Specifies the path to the source directory. If not specified, the source directory will be set to
    a directory named "module" in the directory above this script. i.e. $PSScriptRoot\..\module

.PARAMETER ClassFolderName
    Specifies the name(s) of folder(s) within the source directory that contain classes. If not specified,
    this script will look for a directory named "Classes".

.PARAMETER FunctionFolderName
    Specifies the name(s) of folder(s) within the source directory that contain functions. If not specified,
    this script will look for directories named "Public" and/or "Private".

.PARAMETER ExportClasses
    If specified, classes will be included in the main .psm1 file.  By default, classes are outputted
    into a different file that is loaded by the .psm1 which disables importing classes.

.EXAMPLE
    PS C:\> .\BuildMonolith.ps1 -OutputDirectory .\Release\EditorServicesCommandSuite\1.0.0 -ModuleName EditorServicesCommandSuite

    Builds the monolith module file for this module.

.NOTES
    - Only classes and functions in the root ScriptBlockAst will be extracted

    - Because namespaces are combined, it's possible this process could cause type resolution conflicts
#>

using namespace System.Collections.Generic
using namespace System.IO
using namespace System.Management.Automation
using namespace System.Management.Automation.Language
using namespace Microsoft.PowerShell.Commands

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $ModuleName,

    [ValidateNotNullOrEmpty()]
    [string] $SourcePath,

    [ValidateNotNullOrEmpty()]
    [string[]] $ClassFolderName = 'Classes',

    [ValidateNotNullOrEmpty()]
    [string[]] $FunctionFolderName = ('Public', 'Private'),

    [switch] $ExportClasses
)
begin {
    # Sort using statements by type (namespace/module/assembly) then by System/Other then alphabetically.
    function GetUsingStatementText {
        param([String[]] $Statements)
        end {
            $regex = 'using (namespace|assembly|module) '

            $groupedByType = $Statements | Group-Object {
                $PSItem |
                    Select-String -Pattern $regex -AllMatches |
                    ForEach-Object { $PSItem.Matches[0].Groups[1].Value }
            }

            $groupedText = $groupedByType |
                Sort-Object Name |
                ForEach-Object {
                    $PSItem.Group |
                        Group-Object { ($PSItem -replace $regex).StartsWith('System') } |
                        Sort-Object Name -Descending |
                        ForEach-Object { ($PSItem.Group | Sort-Object) } |
                        Out-String
                }

            return $groupedText | Out-String
        }
    }

    # Resolve paths from parameters and handle errors.
    function ResolvePath {
        param([string] $Path, [string] $VariableName, [switch] $Directory)
        end {
            try {
                $resolved = Resolve-Path $Path -ErrorAction Stop
            } catch {
                $exception = [ItemNotFoundException]::new([UtilityResources]::PathDoesNotExist -f $Path)
                throw [ErrorRecord]::new(
                    <# exception:     #> $exception,
                    <# errorId:       #> 'PathNotFound',
                    <# errorCategory: #> 'ObjectNotFound',
                    <# targetObject:  #> $Path)
            }

            if ($Directory.IsPresent -and -not [Directory]::Exists($resolved)) {
                $exception = [PSArgumentException]::new(
                    'The value specified for the parameter "{0}" must be a directory.' -f $VariableName)
                throw [ErrorRecord]::new(
                    <# exception:     #> $exception,
                    <# errorId:       #> 'DirectoryNotFound',
                    <# errorCategory: #> 'InvalidArgument',
                    <# targetObject:  #> $resolved)
            }

            return $resolved
        }
    }
}
end {
    if (-not $SourcePath) {
        $SourcePath = "$PSScriptRoot\..\module"
    }

    $SourcePath = ResolvePath $SourcePath -VariableName SourcePath -Directory
    $OutputPath = ResolvePath $OutputPath -VariableName OutputPath -Directory

    $header = @'
<#
    This file was generated during the build process to decrease module load
    time.  You can see the original source, organized into separate files at
    the GitHub repository for this project.
#>


'@

    $usings = @{
        Class    = [HashSet[string]]::new()
        Function = [HashSet[string]]::new()
    }

    $commands = @{
        Class    = [List[TypeDefinitionAst]]::new()
        Function = [List[FunctionDefinitionAst]]::new()
    }

    $folders = @{
        Class    = $ClassFolderName
        Function = $FunctionFolderName
    }

    foreach ($commandType in 'function', 'class') {
        foreach ($commandFolder in $folders.$commandType) {
            try {
                $commandFolder = Join-Path $SourcePath -ChildPath $commandFolder -Resolve -ErrorAction Stop
            } catch {
                $PSCmdlet.WriteDebug("Could not resolve $commandType directory '$commandFolder', skipping.")
                continue
            }

            $files = Get-ChildItem $commandFolder\*.ps1
            if (-not $files) {
                $PSCmdlet.WriteDebug("No ps1 files found in class directory '$commandFolder', skipping.")
                continue
            }

            foreach ($file in $files) {
                $ast = [Parser]::ParseFile(
                    <# fileName: #> $file.FullName,
                    <# tokens:   #> [ref]$null,
                    <# errors:   #> [ref]$null)

                $astType = $commands.$commandType.GetType().GetGenericArguments()[0]
                $validAsts = $ast.FindAll({ param($a) $a -is $astType }, $false)
                if ($validAsts) {
                    $commands.$commandType.AddRange($validAsts.ToArray().ForEach($astType))
                }

                $usingAsts = $ast.FindAll({ param($a) $a -is [UsingStatementAst] }, $true)
                foreach ($usingAst in $usingAsts) {
                    $null = $usings.$commandType.Add($usingAst.ToString())
                }
            }
        }
    }

    if ($ExportClasses.IsPresent) {
        $usings.Function.UnionWith($usings.Class)
        $allCommands = [List[StatementAst]]::new($commands.Class)
        $allCommands.AddRange($commands.Function)
        $commands.Function = $allCommands
    } else {
        $classFileName = '{0}.Classes.ps1' -f $ModuleName
        $content = $header
        $content += GetUsingStatementText -Statements $usings.Class
        $content += $commands.Class.Extent.Text -join ([Environment]::NewLine + [Environment]::NewLine)

        $classOutputFile = Join-Path $OutputPath -ChildPath $classFileName
        $content | Out-File -Encoding default -FilePath $classOutputFile
    }

    $moduleOutputFile = (Join-Path $OutputPath -ChildPath $ModuleName) + '.psm1'
    $content = $header
    $content += GetUsingStatementText -Statements $usings.Function

    $injection = [string]::Empty
    if (-not $ExportClasses.IsPresent) {
        $injection += ('. "$PSScriptRoot\{0}"' -f $classFileName) +
            [Environment]::NewLine +
            [Environment]::NewLine
    }

    $injection += $commands.Function.Extent.Text -join ([Environment]::NewLine + [Environment]::NewLine)

    # Use a match evaluator instead of plain text so we don't have to worry about escaping capture groups.
    $content += [regex]::Replace(
        <# input:     #> (Get-Content -Raw $SourcePath\$ModuleName.psm1),
        <# pattern:   #> '# ~MONOLITH_INJECT_START~.+?# ~MONOLITH_INJECT_END~',
        <# evaluator: #> { $injection },
        <# options:   #> 'SingleLine')

    $content | Out-File -Encoding default -FilePath $moduleOutputFile -NoNewline
}
