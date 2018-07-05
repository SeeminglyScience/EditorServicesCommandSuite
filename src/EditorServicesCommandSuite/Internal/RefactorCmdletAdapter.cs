using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using Microsoft.PowerShell.Cmdletization;

namespace EditorServicesCommandSuite.Internal
{
    public class RefactorCmdletAdapter : CmdletAdapter<object>
    {
        private readonly CancellationTokenSource _isStopping = new CancellationTokenSource();

        private IDocumentRefactorProvider _provider;

        public override void BeginProcessing()
        {
        }

        public override void ProcessRecord(object objectInstance, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
        }

        public override void ProcessRecord(QueryBuilder query)
        {
        }

        public override void ProcessRecord(QueryBuilder query, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
        }

        public override void ProcessRecord(MethodInvocationInfo methodInvocationInfo)
        {
            RemoveCommonParameters(Cmdlet.MyInvocation.BoundParameters);
            InvokeRefactorAsync(methodInvocationInfo.MethodName, Cmdlet)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }

        public override void EndProcessing()
        {
        }

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
