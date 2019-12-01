using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Inference;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Reflection;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsData.Expand, "MemberExpression")]
    [RefactorConfiguration(typeof(ExpandMemberExpressionSettings))]
    internal class ExpandMemberExpressionRefactor : RefactorProvider
    {
        private readonly IRefactorUI _ui;

        internal ExpandMemberExpressionRefactor(IRefactorUI ui)
        {
            _ui = ui;
        }

        public override string Name { get; } = ExpandMemberExpressionStrings.ProviderDisplayName;

        public override string Description { get; } = ExpandMemberExpressionStrings.ProviderDisplayDescription;

        public override ImmutableArray<CodeAction> SupportedActions { get; } = ImmutableArray.Create(
            CodeAction.Inactive(CodeActionIds.ExpandMemberExpression, "Expand method invocation"),
            CodeAction.Inactive(CodeActionIds.ExpandMemberExpression, "Expand method invocation - include non-public"));

        private CodeAction DefaultCodeAction => SupportedActions[0];

        private CodeAction IncludeNonPublicCodeAction => SupportedActions[1];

        public override async Task ComputeCodeActions(DocumentContextBase context)
        {
            if (!context.Ast.TryFindParent(maxDepth: 3, out InvokeMemberExpressionAst invokeMember))
            {
                return;
            }

            await context.RegisterCodeActionAsync(
                DefaultCodeAction.With(
                    ExpandMemberExpressionAsync,
                    (invokeMember, _ui, false)))
                .ConfigureAwait(false);

            await context.RegisterCodeActionAsync(
                IncludeNonPublicCodeAction.With(
                    ExpandMemberExpressionAsync,
                    (invokeMember, _ui, true)))
                .ConfigureAwait(false);
        }

        public override async Task Invoke(DocumentContextBase context)
        {
            if (!context.Ast.TryFindParent(maxDepth: 3, out InvokeMemberExpressionAst invokeMember))
            {
                return;
            }

            var config = context.GetConfiguration<ExpandMemberExpressionSettings>();
            CodeAction codeAction = config.AllowNonPublicMembers.IsPresent
                ? IncludeNonPublicCodeAction
                : DefaultCodeAction;

            await ProcessActionForInvoke(
                context,
                codeAction.With(
                    ExpandMemberExpressionAsync,
                    (invokeMember, _ui, config.AllowNonPublicMembers.IsPresent)))
                    .ConfigureAwait(false);
        }

        internal static Task<IEnumerable<DocumentEdit>> GetEdits(
            MemberExpressionAst ast,
            MemberInfo resolvedMember,
            Token memberAccessOperator)
        {
            var expr = new PowerShellScriptWriter(ast);
            if (ast.Expression is TypeExpressionAst typeExpression &&
                !MemberUtil.IsTypeExpressible(resolvedMember.ReflectedType))
            {
                expr.StartWriting(ast.Expression);
                expr.WriteTypeExpression(
                    new PSTypeName(resolvedMember.ReflectedType),
                    allowNonPublic: true);
                expr.FinishWriting();
            }

            if (resolvedMember is EventInfo)
            {
                return Task.FromResult(expr.Edits);
            }

            if (resolvedMember is FieldInfo field && field.IsPublic)
            {
                return Task.FromResult(expr.Edits);
            }

            if (resolvedMember is PropertyInfo property && property.GetGetMethod() != null)
            {
                return Task.FromResult(expr.Edits);
            }

            MethodBase resolvedMethod = resolvedMember as MethodBase;
            if (resolvedMethod == null || !resolvedMethod.IsPublic)
            {
                // For non-public members we want to get rid of the static member access
                // operator (::) if it exists, or add `.GetType()` if it doesn't.
                if (ast.Static && memberAccessOperator.Kind == TokenKind.ColonColon)
                {
                    expr.StartWriting(memberAccessOperator);
                    expr.Write(Symbols.Dot);
                    expr.FinishWriting();
                }
                else
                {
                    expr.SetPosition(ast.Member);
                    expr.Write("GetType");
                    expr.WriteChars(Symbols.ParenOpen, Symbols.ParenClose, Symbols.Dot);
                    expr.CreateDocumentEdits();
                }
            }

            if (resolvedMethod != null)
            {
                expr.StartWriting(ast.Member);
                if (resolvedMethod.IsPublic &&
                    (!resolvedMethod.IsConstructor || resolvedMethod.ReflectedType.IsPublic))
                {
                    WritePublicMethod(expr, resolvedMethod);
                }
                else
                {
                    string instance = resolvedMethod.IsStatic ? "$null" : ast.Expression.ToString();
                    WriteNonPublicMethod(expr, resolvedMethod, instance);
                }

                expr.FinishWriting();
                return Task.FromResult(expr.Edits);
            }

            // Write properties and fields access via reflection. Example:
            // <instance|type>.GetType().
            //     .Get<Field|Property>("<fieldName>", [BindingFlags]'<Instance|Static>, NonPublic').
            //     .GetValue(<instance|$null>)
            expr.StartWriting(ast.Member);
            expr.StartMethodChain()
                .Method("Get{0}".FormatInvariant(resolvedMember.MemberType))
                    .Argument(arg => arg.WriteStringExpression(StringConstantType.SingleQuoted, resolvedMember.Name))
                    .Argument(arg => arg.WriteEnum(MemberUtil.GetBindingFlags(resolvedMember)))
                .Method("GetValue")
                    .Argument(arg =>
                    {
                        if (ast.Static)
                        {
                            arg.WriteVariable("null");
                            return;
                        }

                        arg.Write(ast.Expression.ToString());
                    })
                .Complete();
            expr.FinishWriting();
            return Task.FromResult(expr.Edits);
        }

        private static void WritePublicMethod(PowerShellScriptWriter expr, MethodBase method)
        {
            expr.StartMethodChain()
                .Method(method.IsConstructor ? "new" : method.Name, skipMethodNameNewLine: true)
                .ArgumentsOnNewLines()
                .Arguments(
                    method.GetParameters(),
                    (arg, parameter) =>
                    {
                        arg.WriteExplicitMethodParameterName(parameter.Name);
                        arg.WriteVariable(parameter.Name);
                    })
                .Complete();
        }

        private static void WriteNonPublicMethod(PowerShellScriptWriter expr, MethodBase method, string instance)
        {
            ParameterInfo[] parameters = method.GetParameters();
            MethodChainWriter chain = WriteGetMember(expr.StartMethodChain(), method).Method("Invoke");
            if (!method.IsConstructor)
            {
                chain.Argument(arg => arg.Write(instance));
            }

            bool shouldWriteExplicit = parameters.Length > 2;
            chain.Argument(arg =>
            {
                arg.WriteChars(Symbols.At, Symbols.ParenOpen);
                if (shouldWriteExplicit)
                {
                    arg.PushIndent();
                    arg.WriteLine();
                    arg.WriteEachWithSeparator(
                        parameters,
                        param =>
                        {
                            arg.WriteExplicitMethodParameterName(param.Name);
                            arg.WriteVariable(param.Name);
                        },
                        () => arg.WriteLine(Symbols.Comma));
                    arg.PopIndent();
                }
                else
                {
                    arg.WriteEachWithSeparator(
                        parameters,
                        param => arg.WriteVariable(param.Name),
                        Symbols.MethodParameterSeparator);
                }

                arg.Write(Symbols.ParenClose);
            }).Complete();
        }

        private static MethodChainWriter WriteGetMember(MethodChainWriter expr, MethodBase method)
        {
            string getMethod = "Get" + method.MemberType.ToString();
            BindingFlags flags = MemberUtil.GetBindingFlags(method);
            bool requiresExplicit = method.ReflectedType.GetMember(method.Name, flags).Length > 1;
            if (!requiresExplicit)
            {
                expr.Method(getMethod);
                if (!method.IsConstructor)
                {
                    expr.Argument(
                        arg => arg.WriteStringExpression(
                            StringConstantType.SingleQuoted,
                            method.Name));
                }

                return expr.Argument(arg => arg.WriteEnum(flags));
            }

            expr.Method(getMethod).ArgumentsOnNewLines();
            if (!method.IsConstructor)
            {
                expr.Argument(
                    arg => arg.WriteStringExpression(
                        StringConstantType.SingleQuoted,
                        method.Name),
                    "name");
            }

            ParameterInfo[] parameters = method.GetParameters();
            expr.Argument(arg => arg.WriteEnum(flags), "bindingAttr")
                .Argument(arg => arg.WriteVariable("null"), "binder")
                .Argument(
                    arg =>
                    {
                        if (parameters.Length == 0)
                        {
                            arg.WriteStaticMemberExpression(typeof(System.Type), "EmptyTypes");
                            return;
                        }

                        arg.WriteChars(Symbols.At, Symbols.ParenOpen);
                        arg.WriteEachWithSeparator(
                            parameters,
                            param => arg.WriteTypeExpression(param.ParameterType, allowNonPublic: true),
                            Symbols.MethodParameterSeparator);
                        arg.Write(Symbols.ParenClose);
                    },
                    "types")
                .Argument(
                    arg =>
                    {
                        if (parameters.Length == 0)
                        {
                            arg.WriteConstructor(typeof(ParameterModifier[]), () => arg.Write('0'));
                            return;
                        }

                        arg.WriteTypeExpression(typeof(ParameterModifier[]));
                        arg.Write(parameters.Length.ToString());
                    },
                    "modifier");

            return expr;
        }

        private static string GetDescriptionForMember(MemberInfo member)
        {
            // Build a display string similar to:
            // 1 - MethodName(ParameterType parameterName, ParameterType2 parameterName2)
            var sb = new StringBuilder(member.Name);
            if (!(member is MethodBase method))
            {
                return sb.ToString();
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length == 0)
            {
                return sb.Append(Symbols.ParenOpen).Append(Symbols.ParenClose).ToString();
            }

            sb.Append(Symbols.ParenOpen);
            for (var i = 0; i < parameters.Length; i++)
            {
                MemberUtil.GetTypeNameForLiteralImpl(
                    parameters[i].ParameterType,
                    dropNamespaces: true,
                    new HashSet<string>(),
                    sb,
                    skipGenericArgs: false);

                sb.Append(Symbols.Space).Append(parameters[i].Name);
                if (i != parameters.Length - 1)
                {
                    sb.Append(Symbols.MethodParameterSeparator);
                }
            }

            return sb.Append(Symbols.ParenClose).ToString();
        }

        private static async Task ExpandMemberExpressionAsync(
            DocumentContextBase context,
            InvokeMemberExpressionAst invokeMember,
            IRefactorUI ui,
            bool includeNonPublic)
        {
            ExpandMemberExpressionSettings config = context.GetConfiguration<ExpandMemberExpressionSettings>();
            MemberInfo[] inferredMembers =
                    (await invokeMember.GetInferredMembersAsync(
                        context.PipelineThread,
                        skipArgumentCheck: true,
                        includeNonPublic: includeNonPublic,
                        context.CancellationToken)
                        .ConfigureAwait(false))
                    .ToArray();

            if (inferredMembers.Length == 0)
            {
                await ui.ShowErrorMessageOrThrowAsync(
                    Error.CannotInferMember,
                    invokeMember.Member)
                    .ConfigureAwait(false);
                return;
            }

            MemberInfo chosenMember = await ui.ShowChoicePromptAsync(
                ExpandMemberExpressionStrings.OverloadChoiceCaption,
                ExpandMemberExpressionStrings.OverloadChoiceMessage,
                inferredMembers,
                GetDescriptionForMember)
                .ConfigureAwait(false);

            IEnumerable<DocumentEdit> edits = await GetEdits(
                invokeMember,
                chosenMember,
                context.Token.At(invokeMember.Expression, atEnd: true).Next.Value)
                .ConfigureAwait(false);

            await context.RegisterWorkspaceChangeAsync(
                WorkspaceChange.EditDocument(
                    context.Document,
                    edits))
                .ConfigureAwait(false);
        }
    }
}
