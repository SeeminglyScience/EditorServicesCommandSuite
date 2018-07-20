using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Inference
{
    internal class AstTypeInference
    {
        internal static IList<PSTypeName> InferTypeOf(
            Ast ast,
            IPowerShellExecutor powerShell,
            EngineIntrinsics engine,
            bool includeNonPublic = false)
        {
            // This entire method is a very temporary hack, and will be replaced once
            // PowerShell/PowerShell#7279 is answered.
            (Ast rootAst, Token[] tokens, IScriptPosition position) = CommandCompletion
                .MapStringInputToParsedInput(
                    ast.Extent.StartScriptPosition.GetFullScript(),
                    ast.Extent.EndOffset);

            var completionAnalysisArgs = new object[]
            {
                rootAst,
                tokens,
                position,
                new System.Collections.Hashtable(),
            };

            var completionAnalysis = typeof(PSObject).Assembly
                .GetType("System.Management.Automation.CompletionAnalysis")
                .InvokeMember(
                    name: null,
                    invokeAttr: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance,
                    binder: null,
                    target: null,
                    args: completionAnalysisArgs);

            var executionContext = engine.GetType()
                .GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(engine);

            var completionContext = completionAnalysis.GetType()
                .GetMethod("CreateCompletionContext", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(completionAnalysis, new[] { executionContext });

            return ((IEnumerable<PSTypeName>)ast.GetType()
                .GetMethod("GetInferredType", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(ast, new[] { completionContext }))
                ?.Where(t => t.Type != typeof(object))
                ?.ToArray()
                ?? Empty.Array<PSTypeName>();
        }
    }
}
