using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite.Utility
{
    internal static class MiscExtensions
    {
        internal static void SynchronousAwait(this Task task, CancellationToken cancellationToken = default)
        {
            task.WaitForCompletion(cancellationToken);
            task.ConfigureAwait(continueOnCapturedContext: false)
                .GetAwaiter()
                .GetResult();
        }

        internal static TResult SynchronousAwait<TResult>(this Task<TResult> task, CancellationToken cancellationToken = default)
        {
            task.WaitForCompletion(cancellationToken);
            return task.ConfigureAwait(continueOnCapturedContext: false)
                .GetAwaiter()
                .GetResult();
        }

        internal static void WaitForCompletion(this IAsyncResult task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                task.AsyncWaitHandle.WaitOne();
                return;
            }

            var waitHandles = new WaitHandle[]
            {
                task.AsyncWaitHandle,
                cancellationToken.WaitHandle,
            };

            int handleIndex = WaitHandle.WaitAny(waitHandles);
            if (handleIndex == 1)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }

        internal static void ThrowIfStopping(this PSCmdlet cmdlet)
        {
            if (cmdlet == null || !cmdlet.Stopping)
            {
                return;
            }

            throw new PipelineStoppedException();
        }

        internal static PSObject Invoke(this PSObject pso, string methodName, params object[] args)
        {
            return PSObject.AsPSObject(
                pso.Methods[methodName]?.Invoke(args)
                ?? AutomationNull.Value);
        }

        internal static string FormatInvariant(this string format, object arg0)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                format,
                arg0);
        }

        internal static string FormatInvariant(this string format, object arg0, object arg1)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                format,
                arg0,
                arg1);
        }

        internal static string FormatInvariant(this string format, object arg0, object arg1, object arg2)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                format,
                arg0,
                arg1,
                arg2);
        }

        internal static string FormatInvariant(this string format, params object[] args)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                format,
                args);
        }

        internal static string FormatCulture(this string format, object arg0)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                format,
                arg0);
        }

        internal static string FormatCulture(this string format, object arg0, object arg1)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                format,
                arg0,
                arg1);
        }

        internal static string FormatCulture(this string format, object arg0, object arg1, object arg2)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                format,
                arg0,
                arg1,
                arg2);
        }

        internal static string FormatCulture(this string format, params object[] args)
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                format,
                args);
        }

        internal static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            Validate.IsNotNull(nameof(source), source);
            Validate.IsNotNull(nameof(action), action);
            foreach (T item in source)
            {
                action(item);
            }
        }
    }
}
