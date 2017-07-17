using namespace Microsoft.PowerShell.EditorServices.Protocol.MessageProtocol
using namespace Microsoft.PowerShell.EditorServices.Protocol.Messages

function ReadInputPrompt {
    param([string]$Prompt)
    end {
        $result = $psEditor.
            Components.
            Get([IMessageSender]).SendRequest(
                [ShowInputPromptRequest]::Type,
                [ShowInputPromptRequest]@{
                    Name  = $Prompt
                    Label = $Prompt
                },
                $true).
            Result

        if (-not $result.PromptCanceled) {
            $result.ResponseText
        }
    }
}
