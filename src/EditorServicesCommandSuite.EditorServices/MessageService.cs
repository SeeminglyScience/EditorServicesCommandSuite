using System;
using System.Threading.Tasks;
using Microsoft.PowerShell.EditorServices.Extensions;
using Microsoft.PowerShell.EditorServices.Protocol.MessageProtocol;

namespace EditorServicesCommandSuite.EditorServices
{
    internal class MessageService
    {
        private readonly EditorObject _psEditor;

        internal MessageService(EditorObject psEditor)
        {
            _psEditor = psEditor ?? throw new ArgumentNullException(nameof(psEditor));
            Sender = (IMessageSender)_psEditor.Components.Get(typeof(IMessageSender));
        }

        internal IMessageSender Sender { get; }

        internal void SendEvent<TParams, TRegistrationOptions>(
            NotificationType<TParams, TRegistrationOptions> eventType,
            TParams eventParams)
        {
            Sender
                .SendEvent(eventType, eventParams)
                .ConfigureAwait(continueOnCapturedContext: false)
                .GetAwaiter()
                .GetResult();
        }

        internal async Task SendEventAsync<TParams, TRegistrationOptions>(
            NotificationType<TParams, TRegistrationOptions> eventType,
            TParams eventParams)
        {
            await Sender
                .SendEvent(eventType, eventParams)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        internal TResult SendRequest<TParams, TResult, TError, TRegistrationOptions>(
            RequestType<TParams, TResult, TError, TRegistrationOptions> requestType,
            TParams requestParams,
            bool waitForResponse)
        {
            return Sender
                .SendRequest(requestType, requestParams, waitForResponse)
                .ConfigureAwait(continueOnCapturedContext: false)
                .GetAwaiter()
                .GetResult();
        }

        internal async Task<TResult> SendRequestAsync<TParams, TResult, TError, TRegistrationOptions>(
            RequestType<TParams, TResult, TError, TRegistrationOptions> requestType,
            TParams requestParams,
            bool waitForResponse)
        {
            return await Sender
                .SendRequest(requestType, requestParams, waitForResponse)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        internal TResult SendRequest<TResult, TError, TRegistrationOptions>(
            RequestType0<TResult, TError, TRegistrationOptions> requestType0)
        {
            return Sender
                .SendRequest(requestType0)
                .ConfigureAwait(continueOnCapturedContext: false)
                .GetAwaiter()
                .GetResult();
        }

        internal async Task<TResult> SendRequestAsync<TResult, TError, TRegistrationOptions>(
            RequestType0<TResult, TError, TRegistrationOptions> requestType0)
        {
            return await Sender
                .SendRequest(requestType0)
                .ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
