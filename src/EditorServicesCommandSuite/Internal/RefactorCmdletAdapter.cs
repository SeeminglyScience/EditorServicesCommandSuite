using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using Microsoft.PowerShell.Cmdletization;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Represents the cmdletization of a refactor provider.
    /// </summary>
    public class RefactorCmdletAdapter : CmdletAdapter<object>
    {
        private readonly CancellationTokenSource _isStopping = new CancellationTokenSource();

        private IDocumentRefactorProvider _provider;

        /// <summary>
        /// The BeginProcessing method.
        /// </summary>
        public override void BeginProcessing()
        {
        }

        /// <summary>
        /// The ProcessRecord method.
        /// </summary>
        /// <param name="objectInstance">The parameter is not used.</param>
        /// <param name="methodInvocationInfo">The parameter is not used.</param>
        /// <param name="passThru">The parameter is not used.</param>
        public override void ProcessRecord(object objectInstance, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
        }

        /// <summary>
        /// The ProcessRecord method.
        /// </summary>
        /// <param name="query">The parameter is not used.</param>
        public override void ProcessRecord(QueryBuilder query)
        {
        }

        /// <summary>
        /// The ProcessRecord method.
        /// </summary>
        /// <param name="query">The parameter is not used.</param>
        /// <param name="methodInvocationInfo">The parameter is not used.</param>
        /// <param name="passThru">The parameter is not used.</param>
        public override void ProcessRecord(QueryBuilder query, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
        }

        /// <summary>
        /// The ProcessRecord method.
        /// </summary>
        /// <param name="methodInvocationInfo">
        /// The information used to determine which refactor provider to target.
        /// </param>
        public override void ProcessRecord(MethodInvocationInfo methodInvocationInfo)
        {
            RemoveCommonParameters(Cmdlet.MyInvocation.BoundParameters);
            InvokeRefactorAsync(methodInvocationInfo.MethodName, Cmdlet)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        /// <summary>
        /// The EndProcessing method.
        /// </summary>
        public override void EndProcessing()
        {
        }

        /// <summary>
        /// The StopProcessing method.
        /// </summary>
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

        private async Task InvokeRefactorAsync(string className, PSCmdlet psCmdlet)
        {
            _provider = CommandSuite.Instance.Refactors.GetProvider(className);
            var configAttribute = _provider.GetType().GetCustomAttributes(
                typeof(RefactorConfigurationAttribute),
                true)
                .OfType<RefactorConfigurationAttribute>()
                .FirstOrDefault();

            DocumentContextBase context =
                await CommandSuite.Instance.DocumentContext
                    .GetDocumentContextAsync(
                        psCmdlet,
                        _isStopping.Token);

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

            await CommandSuite.Instance.Documents.WriteDocumentEditsAsync(
                await _provider.RequestEdits(context));
        }
    }
}
