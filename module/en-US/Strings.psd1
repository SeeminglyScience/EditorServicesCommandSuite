ConvertFrom-StringData @'
SettingCommentMainModuleDirectory=The relative path from the current workspace to the root directory of the module.
SettingCommentSourceManifestPath=The relative path from the current workspace to the main module manifest file.
SettingCommentMarkdownDocsPath=The relative path from the current workspace to the directory where markdown files are stored.
SettingCommentStringLocalizationManifest=The relative path from the current workspace to the string localization psd1 file.

EditorCommandExists=Editor command '{0}' already exists, skipping.
GettingImportedModules=Getting imported modules in the workspace.
CheckingDefaultScope=No modules found, checking default scope instead.
EnumeratingScopesForMember=Enumerating scopes to find a matching member.
VariableFound=Found variable with type '{0}'.
SkippingEditorContext=PowerShell Editor Services API not available, skipping.
InferringFromCompletion=Checking for type using standard command completion.

WhatIfSetExtent=Changing '{0}' to '{1}'
ConfirmSetExtent=Continuing will change the the text of extent '{0}' to '{1}'. Are you sure you want to continue?
ConfirmTitle=Confirm
ShouldReplaceSettingsCaption=Replace existing settings?
ShouldReplaceSettingsMessage=A settings file already exists in the specified folder and the "Force" switch parameter was not specified.  If you continue, existing settings will be overridden.  Do you want to continue?
StringNamePrompt=String Name

MissingAst=Unable to find an AST of type '{0}' at the specified location.
MissingMemberExpressionAst=Unable to find a member expression ast near the current cursor location.
MissingEditorContext=Unable to obtain editor context. Make sure PowerShell Editor Services is running and then try the command again.
ExpandEmptyExtent=Cannot expand the extent with start offset '{0}' for file '{1}' because it is empty.
CannotInferType=Unable to infer type for expression '{0}'.
CannotFindModule=Unable to find the module '{0}' in the current session.
TypeNotFound=Unable to find type [{0}].
TemplateGroupCompileError=Internal module error: Unable to compile default template group. Please file an issue on GitHub.
FailureGettingMarkdown=Unable to generate markdown content. Ensure you have the module PlatyPS installed and the comment based help is formatted correctly.
SettingsFileExists=The settings file for workspace '{0}' already exists.
InvalidSettingValue=The value of the setting '{0}' is invalid.  If you have not already created a settings file for this workspace, you can create one with the 'New-ESCSSettingsFile' function.
InferringFromSession=Checking for command in the current session.
InferringFromWorkspace=Checking for command in the workspace module manifest.
VerboseInvalidManifest=Unable to retrieve module manifest for current workspace.
CannotInferModule=Unable to infer module information for the selected command.
CommandNotInModule=The selected command does not belong to a module.
StringNamePromptFail=You must supply a string name for it to be added to the localization table.  Please try the command again.
'@
