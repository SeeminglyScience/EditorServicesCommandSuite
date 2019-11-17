using System;
using System.Threading.Tasks;

using Microsoft.PowerShell.EditorServices.Services.PowerShellContext;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class MessageService
    {
        private readonly EditorObject _psEditor;

        internal MessageService(EditorObject psEditor)
        {
            _psEditor = psEditor ?? throw new ArgumentNullException(nameof(psEditor));
            Sender = (ILanguageServer)_psEditor.Components.GetService(typeof(ILanguageServer));
        }

        internal ILanguageServer Sender { get; }

        internal void SendEvent<TRequest>(
            ActionMessage<TRequest> eventType,
            TRequest eventParams)
        {
            Sender.SendNotification(eventType.Method, eventParams);
        }

        internal async Task SendEventAsync<TRequest>(
            ActionMessage<TRequest> eventType,
            TRequest eventParams)
        {
            await Task.Run(() => Sender.SendNotification(eventType.Method, eventParams))
                .ConfigureAwait(false);
        }

        internal TResponse SendRequest<TRequest, TResponse>(
            FuncMessage<TRequest, TResponse> requestType,
            TRequest requestParams)
        {
            return Sender.SendRequest<TRequest, TResponse>(requestType.Method, requestParams)
                .ConfigureAwait(continueOnCapturedContext: false)
                .GetAwaiter()
                .GetResult();
        }

        internal async Task<TResponse> SendRequestAsync<TRequest, TResponse>(
            FuncMessage<TRequest, TResponse> requestType,
            TRequest requestParams)
        {
            return await Sender
                .SendRequest<TRequest, TResponse>(requestType.Method, requestParams)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        internal TResponse SendRequest<TResponse>(
            FuncMessage<TResponse> requestType)
        {
            return Sender
                .SendRequest<TResponse>(requestType.Method)
                .ConfigureAwait(continueOnCapturedContext: false)
                .GetAwaiter()
                .GetResult();
        }

        internal async Task<TResponse> SendRequestAsync<TResponse>(
            FuncMessage<TResponse> requestType)
        {
            return await Sender
                .SendRequest<TResponse>(requestType.Method)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        internal void SendRequest<TRequest>(
            ActionMessage<TRequest> requestType,
            TRequest request)
        {
            Sender
                .SendRequest(requestType.Method, request)
                .ConfigureAwait(continueOnCapturedContext: false)
                .GetAwaiter()
                .GetResult();
        }

        internal async Task SendRequestAsync<TRequest>(
            ActionMessage<TRequest> requestType,
            TRequest request)
        {
            await Sender
                .SendRequest(requestType.Method, request)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
