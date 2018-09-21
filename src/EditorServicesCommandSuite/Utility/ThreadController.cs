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

        private readonly BlockingCollection<IJob> _jobs;

        private Thread _thread;

        internal ThreadController(EngineIntrinsics engine, PSCmdlet cmdlet)
        {
            _engine = engine;
            _cmdlet = cmdlet;
            _thread = Thread.CurrentThread;
            _jobs = new BlockingCollection<IJob>();
        }

        /// <summary>
        /// Represents an executable object.
        /// </summary>
        private interface IJob
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
                if (requestTask.IsCompleted)
                {
                    requestTask.GetAwaiter().GetResult();
                }

                throw;
            }
        }

        internal void GiveControl(CancellationToken cancellationToken)
        {
            Debug.Assert(Thread.CurrentThread == _thread, "GiveThread called on wrong thread");

            try
            {
                while (true)
                {
                    foreach (IJob job in _jobs.GetConsumingEnumerable(cancellationToken))
                    {
                        job.Execute();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw new PipelineStoppedException();
            }
        }

        internal async Task InvokeAsync(
            Action action,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(action != null, nameof(action));
            await InvokeAsync((engine, cmdlet) => action());
        }

        internal async Task InvokeAsync(
            Action<EngineIntrinsics> action,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(action != null, nameof(action));
            await InvokeAsync((engine, cmdlet) => action(engine));
        }

        internal async Task InvokeAsync(
            Action<PSCmdlet> action,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(action != null, nameof(action));
            await InvokeAsync((engine, cmdlet) => action(cmdlet));
        }

        internal async Task InvokeAsync(
            Action<EngineIntrinsics, PSCmdlet> action,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(action != null, nameof(action));
            Debug.Assert(Thread.CurrentThread != _thread, "Already on controlled thread");
            var job = new Job<VoidJobResult>(this, action, cancellationToken);
            _jobs.Add(job);
            await job.Completion.Task;
        }

        internal async Task<TResult> InvokeAsync<TResult>(
            Func<TResult> func,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(func != null, nameof(func));
            return await InvokeAsync<TResult>((engine, cmdlet) => func(), cancellationToken);
        }

        internal async Task<TResult> InvokeAsync<TResult>(
            Func<EngineIntrinsics, TResult> func,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(func != null, nameof(func));
            return await InvokeAsync<TResult>((engine, cmdlet) => func(engine), cancellationToken);
        }

        internal async Task<TResult> InvokeAsync<TResult>(
            Func<PSCmdlet, TResult> func,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(func != null, nameof(func));
            return await InvokeAsync<TResult>((engine, cmdlet) => func(cmdlet), cancellationToken);
        }

        internal async Task<TResult> InvokeAsync<TResult>(
            Func<EngineIntrinsics, PSCmdlet, TResult> func,
            CancellationToken cancellationToken = default)
        {
            Debug.Assert(func != null, nameof(func));
            Debug.Assert(Thread.CurrentThread != _thread, "Already on controlled thread");
            var job = new Job<TResult>(this, func, cancellationToken);
            _jobs.Add(job);
            return await job.Completion.Task;
        }

        private readonly struct VoidJobResult
        {
        }

        private class Job<TResult> : IJob
        {
            internal readonly ThreadController Controller;

            internal readonly Func<EngineIntrinsics, PSCmdlet, TResult> Func;

            internal readonly TaskCompletionSource<TResult> Completion;

            internal readonly CancellationToken CancellationToken;

            internal Job(
                ThreadController threadController,
                Action<EngineIntrinsics, PSCmdlet> action,
                CancellationToken cancellationToken)
            {
                Func = (engine, cmdlet) =>
                {
                    action(engine, cmdlet);
                    return default;
                };

                Completion = new TaskCompletionSource<TResult>();
                CancellationToken = cancellationToken;
                Controller = threadController;
            }

            internal Job(
                ThreadController threadController,
                Func<EngineIntrinsics, PSCmdlet, TResult> func,
                CancellationToken cancellationToken)
            {
                Func = func;
                Completion = new TaskCompletionSource<TResult>();
                CancellationToken = cancellationToken;
                Controller = threadController;
            }

            void IJob.Execute()
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    Task.Run(() => Completion.SetCanceled());
                    return;
                }

                try
                {
                    TResult result = Func(Controller._engine, Controller._cmdlet);
                    Task.Run(() => Completion.TrySetResult(result));
                }
                catch (Exception e)
                {
                    Task.Run(() => Completion.TrySetException(e));
                }
            }
        }
    }
}
