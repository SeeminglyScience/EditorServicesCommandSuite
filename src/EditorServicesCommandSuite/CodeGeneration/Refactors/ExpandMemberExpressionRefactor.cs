using System.Collections.Generic;
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
    internal class ExpandMemberExpressionRefactor : AstRefactorProvider<MemberExpressionAst>
    {
        private readonly IRefactorUI _ui;

        internal ExpandMemberExpressionRefactor(IRefactorUI ui)
        {
            _ui = ui;
        }

        public override string Name { get; } = ExpandMemberExpressionStrings.ProviderDisplayName;

        public override string Description { get; } = ExpandMemberExpressionStrings.ProviderDisplayDescription;

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

        internal override async Task<IEnumerable<DocumentEdit>> RequestEdits(
            DocumentContextBase request,
            MemberExpressionAst ast)
        {
            ExpandMemberExpressionSettings config = request.GetConfiguration<ExpandMemberExpressionSettings>();
            MemberInfo[] inferredMembers = ast
                .GetInferredMembers(
                    CommandSuite.Instance,
                    skipArgumentCheck: true,
                    includeNonPublic: config.AllowNonPublicMembers.IsPresent)
                .ToArray();

            if (inferredMembers.Length == 0)
            {
                await _ui?.ShowErrorMessageAsync(
                    ExpandMemberExpressionStrings.CannotInferMember.FormatCulture(ast.Member),
                    waitForResponse: false);
                return Empty.Array<DocumentEdit>();
            }

            MemberInfo chosenMember = await _ui.ShowChoicePromptAsync(
                ExpandMemberExpressionStrings.OverloadChoiceCaption,
                ExpandMemberExpressionStrings.OverloadChoiceMessage,
                inferredMembers,
                GetDescriptionForMember);

            return await GetEdits(
                ast,
                chosenMember,
                request.Token.At(ast.Expression, atEnd: true).Next.Value);
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
            MethodBase method = member as MethodBase;
            if (method == null)
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
    }
}
