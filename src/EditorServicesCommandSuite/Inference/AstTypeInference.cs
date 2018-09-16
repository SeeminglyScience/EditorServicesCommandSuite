// This entire file is a collection of very temporary hacks, and will be replaced once
// PowerShell/PowerShell#7279 is answered.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.Inference
{
    internal class AstTypeInference
    {
        private static InferTypeOfProxy s_inferTypeOfProxy;

        internal delegate IList<PSTypeName> InferTypeOfProxy(Ast ast);

        internal static InferTypeOfProxy InferTypeOfProxyDelegate
        {
            get
            {
                if (s_inferTypeOfProxy != null)
                {
                    return s_inferTypeOfProxy;
                }

                MethodInfo getInferredTypeMethod = typeof(Ast).GetMethod(
                    "GetInferredType",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                if (getInferredTypeMethod != null)
                {
                    return s_inferTypeOfProxy = GetLegacyTypeInference(getInferredTypeMethod);
                }

                return s_inferTypeOfProxy = GetCoreTypeInference();
            }
        }

        internal static IList<PSTypeName> InferTypeOf(
            Ast ast,
            IPowerShellExecutor powerShell,
            EngineIntrinsics engine,
            bool includeNonPublic = false)
        {
            return InferTypeOfProxyDelegate(ast)
                ?.Where(t => t.Type != typeof(object))
                ?.ToArray()
                ?? Array.Empty<PSTypeName>();
        }

        private static InferTypeOfProxy GetCoreTypeInference()
        {
            return (InferTypeOfProxy)typeof(PSObject).Assembly
                .GetType("System.Management.Automation.AstTypeInference")
                .GetMethod(
                    "InferrTypeOf",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Ast) },
                    new[] { new ParameterModifier(1) })
                .CreateDelegate(typeof(InferTypeOfProxy));
        }

        private static InferTypeOfProxy GetLegacyTypeInference(MethodInfo getInferredTypeMethod)
        {
            // Compile a dynamic LINQ expression that builds type inference context and
            // gets the inferred type of an AST using the older Windows PowerShell private
            // API's. The compiled expression is a bit faster, but is pretty unreadable.
            // Thankfully it's also temporary.
            var reflectionInfo = new LegacyTypeInference(getInferredTypeMethod);
            ParameterExpression astParameter = Expression.Parameter(typeof(Ast));
            ParameterExpression completionDataLocal =
                Expression.Variable(typeof(Tuple<Ast, Token[], IScriptPosition>));
            ParameterExpression runspaceLocal = Expression.Variable(typeof(Runspace));

            BinaryExpression assignLocalDataExpr = Expression.Assign(
                completionDataLocal,
                Expression.Call(
                    typeof(CommandCompletion),
                    "MapStringInputToParsedInput",
                    Array.Empty<Type>(),
                    Expression.Call(
                        Expression.Property(
                            Expression.Property(
                                astParameter,
                                "Extent"),
                            "StartScriptPosition"),
                        "GetFullScript",
                        Array.Empty<Type>(),
                        Array.Empty<Expression>()),
                    Expression.Property(
                        Expression.Property(
                            astParameter,
                            "Extent"),
                        "StartOffset")));

            BlockExpression getInferredTypeExpr = Expression.Block(
                new[] { runspaceLocal },
                Expression.Assign(
                    runspaceLocal,
                    Expression.Call(
                        typeof(RunspaceFactory),
                        "CreateRunspace",
                        Array.Empty<Type>(),
                        Array.Empty<Expression>())),
                Expression.TryFinally(
                    Expression.Block(
                        Expression.Call(runspaceLocal, "Open", Array.Empty<Type>(), Array.Empty<Expression>()),
                        Expression.Call(
                            typeof(Enumerable),
                            "ToArray",
                            new[] { typeof(PSTypeName) },
                            Expression.Call(
                                astParameter,
                                reflectionInfo.Ast_GetInferredType,
                                Expression.Call(
                                    Expression.New(
                                        reflectionInfo.CompletionAnalysis_ctor,
                                        Expression.Property(completionDataLocal, "Item1"),
                                        Expression.Property(completionDataLocal, "Item2"),
                                        Expression.Property(completionDataLocal, "Item3"),
                                        Expression.New(typeof(System.Collections.Hashtable))),
                                    reflectionInfo.CompletionAnalysis_CreateCompletionContext,
                                    Expression.Property(runspaceLocal, "ExecutionContext"))))),
                    Expression.Call(runspaceLocal, "Dispose", Array.Empty<Type>(), Array.Empty<Expression>())));

            return Expression.Lambda<InferTypeOfProxy>(
                Expression.Block(
                    new[] { completionDataLocal },
                    assignLocalDataExpr,
                    getInferredTypeExpr),
                new[] { astParameter })
                .Compile();
        }

        private struct LegacyTypeInference
        {
            internal readonly Type CompletionAnalysis;

            internal readonly ConstructorInfo CompletionAnalysis_ctor;

            internal readonly MethodInfo CompletionAnalysis_CreateCompletionContext;

            internal readonly FieldInfo EngineIntrinsics__context;

            internal readonly MethodInfo Ast_GetInferredType;

            internal LegacyTypeInference(MethodInfo getInferredTypeMethod)
            {
                CompletionAnalysis = typeof(PSObject).Assembly
                    .GetType("System.Management.Automation.CompletionAnalysis");

                CompletionAnalysis_ctor = CompletionAnalysis.GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(Ast), typeof(Token[]), typeof(IScriptPosition), typeof(System.Collections.Hashtable) },
                    new[] { new ParameterModifier(4) });

                CompletionAnalysis_CreateCompletionContext = CompletionAnalysis.GetMethod(
                    "CreateCompletionContext",
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(PSObject).Assembly.GetType("System.Management.Automation.ExecutionContext") },
                    new[] { new ParameterModifier(1) });

                EngineIntrinsics__context = typeof(EngineIntrinsics).GetField(
                    "_context",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Ast_GetInferredType = getInferredTypeMethod;
            }
        }
    }
}
