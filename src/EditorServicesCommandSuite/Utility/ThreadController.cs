using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Utility
{
    internal class ThreadController
    {
        private readonly EngineIntrinsics _engine;

        private readonly PSCmdlet _cmdlet;

        private readonly BlockingCollection<IThreadExecutionRequest> _requestQueue;

        private readonly int _threadId;

        internal ThreadController(EngineIntrinsics engine, PSCmdlet cmdlet)
        {
            _engine = engine;
            _cmdlet = cmdlet;
            _threadId = Thread.CurrentThread.ManagedThreadId;
            _requestQueue = new BlockingCollection<IThreadExecutionRequest>();
        }

        /// <summary>
        /// Represents an executable object.
        /// </summary>
        private interface IThreadExecutionRequest
        {
            /// <summary>
            /// Executes the delegate associated with this object.
            /// </summary>
            void Execute();
        }

        internal void GiveControl(Task requestTask, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CancellationTokenSource cancelWhenCompleted =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestTask.ContinueWith(_ => cancelWhenCompleted.Cancel());

            try
            {
                GiveControl(cancelWhenCompleted.Token);
            }
            catch (PipelineStoppedException)
            {
                // Allow exceptions from the request task to surface if it's completed.
                if (requestTask.IsCompleted)
                {
                    requestTask.GetAwaiter().GetResult();
                }

                throw;
            }
        }

        internal void GiveControl(CancellationToken cancellationToken)
        {
            Debug.Assert(IsControllingCurrentThread(), "GiveThread called on wrong thread");

            try
            {
                while (true)
                {
                    foreach (IThreadExecutionRequest request in _requestQueue.GetConsumingEnumerable(cancellationToken))
                    {
                        request.Execute();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw new PipelineStoppedException();
            }
        }

        /// <summary>
        /// Get the <see cref="EngineIntrinsics" /> and <see cref="PSCmdlet" /> instances
        /// specific to the controlled thread.
        /// </summary>
        /// <remarks>
        /// This should only be called when already on the controlled thread.
        /// </remarks>
        /// <returns>The thread context.</returns>
        internal (EngineIntrinsics, PSCmdlet) GetThreadContext()
        {
            Debug.Assert(IsControllingCurrentThread(), "Thread context retrieved on wrong thread");
            return (_engine, _cmdlet);
        }

        internal void Invoke(
            Action action,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(action != null, nameof(action));

            if (IsControllingCurrentThread())
            {
                action();
                return;
            }

            var request = RequestFactory.Create(action, cancellationToken);
            _requestQueue.Add(request);
            request.GetResult().SynchronousAwait(cancellationToken);
        }

        internal void Invoke(
            Action<EngineIntrinsics> action,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(action != null, nameof(action));

            if (IsControllingCurrentThread())
            {
                action(_engine);
                return;
            }

            var request = RequestFactory.Create(action, _engine, cancellationToken);
            _requestQueue.Add(request);
            request.GetResult().SynchronousAwait(cancellationToken);
        }

        internal void Invoke(
            Action<PSCmdlet> action,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(action != null, nameof(action));

            if (IsControllingCurrentThread())
            {
                action(_cmdlet);
                return;
            }

            var request = RequestFactory.Create(action, _cmdlet, cancellationToken);
            _requestQueue.Add(request);
            request.GetResult().SynchronousAwait(cancellationToken);
        }

        internal void Invoke(
            Action<EngineIntrinsics, PSCmdlet> action,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(action != null, nameof(action));

            if (IsControllingCurrentThread())
            {
                action(_engine, _cmdlet);
                return;
            }

            var request = RequestFactory.Create(action, _engine, _cmdlet, cancellationToken);
            _requestQueue.Add(request);
            request.GetResult().SynchronousAwait(cancellationToken);
        }

        internal TResult Invoke<TResult>(
            Func<TResult> func,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(func != null, nameof(func));

            if (IsControllingCurrentThread())
            {
                return func();
            }

            var request = RequestFactory.Create(func, cancellationToken);
            _requestQueue.Add(request);
            return request.GetResult().SynchronousAwait(cancellationToken);
        }

        internal TResult Invoke<TResult>(
            Func<EngineIntrinsics, TResult> func,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(func != null, nameof(func));

            if (IsControllingCurrentThread())
            {
                return func(_engine);
            }

            var request = RequestFactory.Create(func, _engine, cancellationToken);
            _requestQueue.Add(request);
            return request.GetResult().SynchronousAwait(cancellationToken);
        }

        internal TResult Invoke<TResult>(
            Func<PSCmdlet, TResult> func,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(func != null, nameof(func));

            if (IsControllingCurrentThread())
            {
                return func(_cmdlet);
            }

            var request = RequestFactory.Create(func, _cmdlet, cancellationToken);
            _requestQueue.Add(request);
            return request.GetResult().SynchronousAwait(cancellationToken);
        }

        internal TResult Invoke<TResult>(
            Func<EngineIntrinsics, PSCmdlet, TResult> func,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(func != null, nameof(func));

            if (IsControllingCurrentThread())
            {
                return func(_engine, _cmdlet);
            }

            var request = RequestFactory.Create(func, _engine, _cmdlet, cancellationToken);
            _requestQueue.Add(request);
            return request.GetResult().SynchronousAwait(cancellationToken);
        }

        internal async Task InvokeAsync(
            Action action,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(action != null, nameof(action));
            Debug.Assert(!IsControllingCurrentThread(), "Already on controlled thread");
            var request = RequestFactory.Create(action, cancellationToken);
            _requestQueue.Add(request);
            await request.GetResult().ConfigureAwait(false);
        }

        internal async Task InvokeAsync(
            Action<EngineIntrinsics> action,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(action != null, nameof(action));
            Debug.Assert(!IsControllingCurrentThread(), "Already on controlled thread");
            var request = RequestFactory.Create(action, _engine, cancellationToken);
            _requestQueue.Add(request);
            await request.GetResult().ConfigureAwait(false);
        }

        internal async Task InvokeAsync(
            Action<PSCmdlet> action,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(action != null, nameof(action));
            Debug.Assert(!IsControllingCurrentThread(), "Already on controlled thread");
            var request = RequestFactory.Create(action, _cmdlet, cancellationToken);
            _requestQueue.Add(request);
            await request.GetResult().ConfigureAwait(false);
        }

        internal async Task InvokeAsync(
            Action<EngineIntrinsics, PSCmdlet> action,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(action != null, nameof(action));
            Debug.Assert(!IsControllingCurrentThread(), "Already on controlled thread");
            var request = RequestFactory.Create(action, _engine, _cmdlet, cancellationToken);
            _requestQueue.Add(request);
            await request.GetResult().ConfigureAwait(false);
        }

        internal async Task<TResult> InvokeAsync<TResult>(
            Func<TResult> func,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(func != null, nameof(func));
            Debug.Assert(!IsControllingCurrentThread(), "Already on controlled thread");
            var request = RequestFactory.Create(func, cancellationToken);
            _requestQueue.Add(request);
            return await request.GetResult().ConfigureAwait(false);
        }

        internal async Task<TResult> InvokeAsync<TResult>(
            Func<EngineIntrinsics, TResult> func,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(func != null, nameof(func));
            Debug.Assert(!IsControllingCurrentThread(), "Already on controlled thread");
            var request = RequestFactory.Create(func, _engine, cancellationToken);
            _requestQueue.Add(request);
            return await request.GetResult().ConfigureAwait(false);
        }

        internal async Task<TResult> InvokeAsync<TResult>(
            Func<PSCmdlet, TResult> func,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(func != null, nameof(func));
            Debug.Assert(!IsControllingCurrentThread(), "Already on controlled thread");
            var request = RequestFactory.Create(func, _cmdlet, cancellationToken);
            _requestQueue.Add(request);
            return await request.GetResult().ConfigureAwait(false);
        }

        internal async Task<TResult> InvokeAsync<TResult>(
            Func<EngineIntrinsics, PSCmdlet, TResult> func,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(func != null, nameof(func));
            Debug.Assert(!IsControllingCurrentThread(), "Already on controlled thread");
            var request = RequestFactory.Create(func, _engine, _cmdlet, cancellationToken);
            _requestQueue.Add(request);
            return await request.GetResult().ConfigureAwait(false);
        }

        private bool IsControllingCurrentThread() => _threadId == Thread.CurrentThread.ManagedThreadId;

        private readonly struct VoidExecutionRequestResult
        {
        }

        private static class RequestFactory
        {
            internal static VoidReturnExecutionRequest Create(
                Action action,
                CancellationToken cancellationToken = default)
            {
                return new VoidReturnExecutionRequest(action, cancellationToken);
            }

            internal static VoidReturnExecutionRequest<TArg1> Create<TArg1>(
                Action<TArg1> action,
                TArg1 arg1,
                CancellationToken cancellationToken = default)
            {
                return new VoidReturnExecutionRequest<TArg1>(action, arg1, cancellationToken);
            }

            internal static VoidReturnExecutionRequest<TArg1, TArg2> Create<TArg1, TArg2>(
                Action<TArg1, TArg2> action,
                TArg1 arg1,
                TArg2 arg2,
                CancellationToken cancellationToken = default)
            {
                return new VoidReturnExecutionRequest<TArg1, TArg2>(action, arg1, arg2, cancellationToken);
            }

            internal static ExecutionRequest<TResult> Create<TResult>(
                Func<TResult> func,
                CancellationToken cancellationToken = default)
            {
                return new ExecutionRequest<TResult>(func, cancellationToken);
            }

            internal static ExecutionRequest<TArg1, TResult> Create<TArg1, TResult>(
                Func<TArg1, TResult> func,
                TArg1 arg1,
                CancellationToken cancellationToken = default)
            {
                return new ExecutionRequest<TArg1, TResult>(func, arg1, cancellationToken);
            }

            internal static ExecutionRequest<TArg1, TArg2, TResult> Create<TArg1, TArg2, TResult>(
                Func<TArg1, TArg2, TResult> func,
                TArg1 arg1,
                TArg2 arg2,
                CancellationToken cancellationToken = default)
            {
                return new ExecutionRequest<TArg1, TArg2, TResult>(func, arg1, arg2, cancellationToken);
            }
        }

        private abstract class ThreadExecutionRequestBase<TResult> : IThreadExecutionRequest
        {
            private protected readonly TaskCompletionSource<TResult> Completion;

            private readonly CancellationToken _cancellationToken;

            internal ThreadExecutionRequestBase(CancellationToken cancellationToken)
            {
                Completion = new TaskCompletionSource<TResult>();
                _cancellationToken = cancellationToken;
            }

            void IThreadExecutionRequest.Execute()
            {
                if (_cancellationToken.IsCancellationRequested)
                {
                    Task.Run(() => Completion.SetCanceled());
                    return;
                }

                try
                {
                    TResult result = ExecuteImpl();
                    Task.Run(() => Completion.TrySetResult(result));
                }
                catch (Exception e)
                {
                    Task.Run(() => Completion.TrySetException(e));
                }
            }

            internal Task<TResult> GetResult()
            {
                return Completion.Task;
            }

            private protected abstract TResult ExecuteImpl();
        }

        private class VoidReturnExecutionRequest : ThreadExecutionRequestBase<VoidExecutionRequestResult>
        {
            private readonly Action _action;

            internal VoidReturnExecutionRequest(Action action, CancellationToken cancellationToken)
                : base(cancellationToken)
            {
                _action = action;
            }

            internal new Task GetResult()
            {
                return Completion.Task;
            }

            private protected override VoidExecutionRequestResult ExecuteImpl()
            {
                _action();
                return default;
            }
        }

        private class VoidReturnExecutionRequest<TArg1> : ThreadExecutionRequestBase<VoidExecutionRequestResult>
        {
            private readonly Action<TArg1> _action;

            private readonly TArg1 _arg1;

            internal VoidReturnExecutionRequest(Action<TArg1> action, TArg1 arg1, CancellationToken cancellationToken)
                : base(cancellationToken)
            {
                _action = action;
                _arg1 = arg1;
            }

            internal new Task GetResult()
            {
                return Completion.Task;
            }

            private protected override VoidExecutionRequestResult ExecuteImpl()
            {
                _action(_arg1);
                return default;
            }
        }

        private class VoidReturnExecutionRequest<TArg1, TArg2> : ThreadExecutionRequestBase<VoidExecutionRequestResult>
        {
            private readonly Action<TArg1, TArg2> _action;

            private readonly TArg1 _arg1;

            private readonly TArg2 _arg2;

            internal VoidReturnExecutionRequest(Action<TArg1, TArg2> action, TArg1 arg1, TArg2 arg2, CancellationToken cancellationToken)
                : base(cancellationToken)
            {
                _action = action;
                _arg1 = arg1;
                _arg2 = arg2;
            }

            internal new Task GetResult()
            {
                return Completion.Task;
            }

            private protected override VoidExecutionRequestResult ExecuteImpl()
            {
                _action(_arg1, _arg2);
                return default;
            }
        }

        private class ExecutionRequest<TResult> : ThreadExecutionRequestBase<TResult>
        {
            internal readonly Func<TResult> Func;

            internal ExecutionRequest(Func<TResult> func, CancellationToken cancellationToken)
                : base(cancellationToken)
            {
                Func = func;
            }

            private protected override TResult ExecuteImpl()
            {
                return Func();
            }
        }

        private class ExecutionRequest<TArg1, TResult> : ThreadExecutionRequestBase<TResult>
        {
            private readonly Func<TArg1, TResult> _func;

            private readonly TArg1 _arg1;

            internal ExecutionRequest(Func<TArg1, TResult> func, TArg1 arg1, CancellationToken cancellationToken)
                : base(cancellationToken)
            {
                _func = func;
                _arg1 = arg1;
            }

            private protected override TResult ExecuteImpl()
            {
                return _func(_arg1);
            }
        }

        private class ExecutionRequest<TArg1, TArg2, TResult> : ThreadExecutionRequestBase<TResult>
        {
            private readonly Func<TArg1, TArg2, TResult> _func;

            private readonly TArg1 _arg1;

            private readonly TArg2 _arg2;

            internal ExecutionRequest(Func<TArg1, TArg2, TResult> func, TArg1 arg1, TArg2 arg2, CancellationToken cancellationToken)
                : base(cancellationToken)
            {
                _func = func;
                _arg1 = arg1;
                _arg2 = arg2;
            }

            private protected override TResult ExecuteImpl()
            {
                return _func(_arg1, _arg2);
            }
        }
    }
}
