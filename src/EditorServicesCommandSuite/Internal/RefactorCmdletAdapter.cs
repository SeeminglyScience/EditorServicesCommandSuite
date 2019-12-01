using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Utility;
using Microsoft.PowerShell.Cmdletization;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Represents the cmdletization of a refactor provider.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class RefactorCmdletAdapter : CmdletAdapter<object>
    {
        private static readonly ConcurrentDictionary<IDocumentRefactorProvider, RefactorConfigurationAttribute> s_configCache =
            new ConcurrentDictionary<IDocumentRefactorProvider, RefactorConfigurationAttribute>();

        private readonly CancellationTokenSource _isStopping = new CancellationTokenSource();

        private IDocumentRefactorProvider _provider;

        /// <summary>
        /// The BeginProcessing method.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void BeginProcessing()
        {
        }

        /// <summary>
        /// The ProcessRecord method.
        /// </summary>
        /// <param name="objectInstance">The parameter is not used.</param>
        /// <param name="methodInvocationInfo">The parameter is not used.</param>
        /// <param name="passThru">The parameter is not used.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void ProcessRecord(object objectInstance, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
        }

        /// <summary>
        /// The ProcessRecord method.
        /// </summary>
        /// <param name="query">The parameter is not used.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void ProcessRecord(QueryBuilder query)
        {
        }

        /// <summary>
        /// The ProcessRecord method.
        /// </summary>
        /// <param name="query">The parameter is not used.</param>
        /// <param name="methodInvocationInfo">The parameter is not used.</param>
        /// <param name="passThru">The parameter is not used.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void ProcessRecord(QueryBuilder query, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
        }

        /// <summary>
        /// The ProcessRecord method.
        /// </summary>
        /// <param name="methodInvocationInfo">
        /// The information used to determine which refactor provider to target.
        /// </param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void ProcessRecord(MethodInvocationInfo methodInvocationInfo)
        {
            RemoveCommonParameters(Cmdlet.MyInvocation.BoundParameters);
            InvokeRefactor(methodInvocationInfo.MethodName, Cmdlet);
        }

        /// <summary>
        /// The EndProcessing method.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void EndProcessing()
        {
        }

        /// <summary>
        /// The StopProcessing method.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void StopProcessing()
        {
            _isStopping.Cancel();
        }

        private static void RemoveCommonParameters(IDictionary<string, object> dictionary)
        {
            foreach (var parameterName in PSCmdlet.CommonParameters)
            {
                dictionary.Remove(parameterName);
            }
        }

        private RefactorConfigurationAttribute GetConfigurationAttribute(IDocumentRefactorProvider provider)
        {
            return provider.GetType()
                .GetCustomAttributes(typeof(RefactorConfigurationAttribute), inherit: true)
                .Cast<RefactorConfigurationAttribute>()
                .FirstOrDefault();
        }

        private void InvokeRefactor(string className, PSCmdlet psCmdlet)
        {
            _provider = CommandSuite.Instance.Refactors.GetProvider(className);
            RefactorConfigurationAttribute configAttribute = s_configCache.GetOrAdd(
                _provider,
                GetConfigurationAttribute);

            var threadController = new ThreadController(
                (EngineIntrinsics)psCmdlet.GetVariableValue("ExecutionContext"),
                psCmdlet);

            Task refactorRequest = Task.Run(
                async () =>
                {
                    DocumentContextBase context =
                        await CommandSuite.Instance.DocumentContext
                            .GetDocumentContextAsync(
                                psCmdlet,
                                _isStopping.Token,
                                threadController)
                                .ConfigureAwait(false);

                    if (configAttribute != null)
                    {
                        RefactorConfiguration configuration =
                            (RefactorConfiguration)LanguagePrimitives.ConvertTo(
                                    Cmdlet.MyInvocation.BoundParameters,
                                    configAttribute.ConfigurationType);

                        context = new ConfiguredDocumentContext<RefactorConfiguration>(
                            configuration,
                            context);
                    }

                    await _provider.Invoke(context).ConfigureAwait(false);
                });

            threadController.GiveControl(refactorRequest, _isStopping.Token);
        }
    }
}
