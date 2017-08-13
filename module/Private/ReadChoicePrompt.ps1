using namespace Microsoft.PowerShell.EditorServices.Protocol.MessageProtocol
using namespace Microsoft.PowerShell.EditorServices.Protocol.Messages
using namespace Microsoft.PowerShell.EditorServices

function ReadChoicePrompt {
    param([string]$Prompt, [System.Management.Automation.Host.ChoiceDescription[]]$Choices)
    end {
        $choiceIndex = 0
        $convertedChoices = $Choices.ForEach{
            $newLabel = '{0} - {1}' -f ($choiceIndex + 1), $PSItem.Label
            [ChoiceDetails]::new($newLabel, $PSItem.HelpMessage)
            $choiceIndex++
        } -as [ChoiceDetails[]]

        $result = $psEditor.
            Components.
            Get([IMessageSender]).SendRequest(
                [ShowChoicePromptRequest]::Type,
                [ShowChoicePromptRequest]@{
                    Caption        = $Prompt
                    Message        = $Prompt
                    Choices        = $convertedChoices
                    DefaultChoices = 0
                },
                $true).
            Result

        if (-not $result.PromptCanceled) {
            # yield
            $result.ResponseText |
                Select-String '^(\d+) - ' |
                ForEach-Object { $PSItem.Matches.Groups[1].Value - 1 }
        }
    }
}
