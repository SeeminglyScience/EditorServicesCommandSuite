namespace EditorServicesCommandSuite.EditorServices
{
#pragma warning disable SA1402
    internal abstract class MessageType
    {
        protected MessageType(string method) => Method = method;

        public string Method { get; }
    }

    internal class ActionMessage<TRequest> : MessageType
    {
        public ActionMessage(string method)
            : base(method)
        {
        }
    }

    internal class FuncMessage<TResponse> : MessageType
    {
        public FuncMessage(string method)
            : base(method)
        {
        }
    }

    internal class FuncMessage<TRequest, TResponse> : MessageType
    {
        public FuncMessage(string method)
            : base(method)
        {
        }
    }
#pragma warning restore SA1402
}
