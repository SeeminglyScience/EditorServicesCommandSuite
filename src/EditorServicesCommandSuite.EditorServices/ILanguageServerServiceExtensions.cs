using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices.Extensions.Services;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;

namespace EditorServicesCommandSuite.EditorServices
{
    internal static class ILanguageServerServiceExtensions
    {
        public static void Show(
            this ILanguageServerService mediator,
            ShowMessageParams @params)
        {
            ShowMessage(mediator, @params);
        }

        public static void Show(this ILanguageServerService mediator, string message)
        {
            ShowMessage(
                mediator,
                new ShowMessageParams()
                {
                    Type = MessageType.Log,
                    Message = message,
                });
        }

        public static void ShowError(this ILanguageServerService mediator, string message)
        {
            ShowMessage(
                mediator,
                new ShowMessageParams()
                {
                    Type = MessageType.Error,
                    Message = message,
                });
        }

        public static void ShowInfo(this ILanguageServerService mediator, string message)
        {
            ShowMessage(
                mediator,
                new ShowMessageParams()
                {
                    Type = MessageType.Info,
                    Message = message,
                });
        }

        public static void ShowMessage(this ILanguageServerService mediator, ShowMessageParams @params)
        {
            mediator.SendNotification(
                WindowNames.ShowMessage,
                @params);
        }

        public static void ShowWarning(this ILanguageServerService mediator, string message)
        {
            ShowMessage(
                mediator,
                new ShowMessageParams()
                {
                    Type = MessageType.Warning,
                    Message = message,
                });
        }

        public static Task<ApplyWorkspaceEditResponse> ApplyEdit(
            this ILanguageServerService mediator,
            ApplyWorkspaceEditParams @params)
        {
            return mediator.SendRequestAsync<ApplyWorkspaceEditParams, ApplyWorkspaceEditResponse>(
                WorkspaceNames.ApplyEdit,
                @params);
        }
    }
}
