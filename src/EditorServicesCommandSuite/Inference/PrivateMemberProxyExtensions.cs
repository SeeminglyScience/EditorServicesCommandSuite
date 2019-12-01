using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace EditorServicesCommandSuite.Inference
{
    internal static class PrivateMemberProxyExtensions
    {
        private static readonly Action<ScriptBlock, SessionState> s_setSessionState;

        static PrivateMemberProxyExtensions()
        {
            s_setSessionState = CreateSetSessionStateDelegate();
        }

        internal static IEnumerable<T> Prepend<T>(this IEnumerable<T> collection, T element)
        {
            yield return element;
            foreach (T t in collection)
                yield return t;
        }

        internal static bool TryGetAstPair(
            this StaticBindingResult bindingResult,
            string parameterName,
            out AstParameterArgumentPair pair)
        {
            pair = null;
            if (string.IsNullOrWhiteSpace(parameterName)) return false;
            if (!bindingResult.BoundParameters.TryGetValue(parameterName, out ParameterBindingResult parameterResult))
            {
                return false;
            }

            try
            {
                pair = AstParameterArgumentPair.Get(parameterResult);
                return true;
            }
            catch (ArgumentNullException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        internal static bool IsReturnTypeVoid(this FunctionMemberAst member)
        {
            return member.ReturnType.TypeName.GetReflectionAttributeType() == typeof(void);
        }

        internal static ReadOnlyCollection<PSTypeName> GetOutputTypes(this ScriptBlock scriptBlock)
        {
            var builder = new ReadOnlyCollectionBuilder<PSTypeName>();
            scriptBlock.GetOutputTypes(builder);
            return builder.ToReadOnlyCollection();
        }

        internal static void GetOutputTypes(this ScriptBlock scriptBlock, IList<PSTypeName> typeList)
        {
            Debug.Assert(typeList != null && !((System.Collections.IList)typeList).IsFixedSize, nameof(typeList));
            Debug.Assert(scriptBlock != null && scriptBlock.Attributes != null, nameof(scriptBlock));

            foreach (Attribute attribute in scriptBlock.Attributes)
            {
                if (!(attribute is OutputTypeAttribute outputTypeAttribute))
                {
                    continue;
                }

                foreach (PSTypeName typeName in outputTypeAttribute.Type)
                {
                    typeList.Add(typeName);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="TypeDefinitionAst"/> from a <see cref="TypeName"/>. This method is
        /// a proxy method to replace access to the _typeDefinitionAst private field.
        /// </summary>
        /// <param name="typeName">The target TypeName.</param>
        /// <returns>The AST of referenced type.</returns>
        internal static TypeDefinitionAst GetTypeDefinition(this TypeName typeName)
        {
            return new PSTypeName(typeName).TypeDefinitionAst;
        }

        internal static bool IsHidden(this PSMemberInfo memberInfo)
        {
            // Really minimal way of guessing if a member is "Hidden". Obviously doesn't work with
            // the keyword.
            return memberInfo.Name.StartsWith("set_", StringComparison.Ordinal) ||
                memberInfo.Name.StartsWith("get_", StringComparison.Ordinal) ||
                memberInfo.Name.StartsWith("add_", StringComparison.Ordinal) ||
                memberInfo.Name.StartsWith("remove_", StringComparison.Ordinal) ||
                memberInfo.MemberType == PSMemberTypes.MemberSet;
        }

        internal static void SetSessionStateInternal(this ScriptBlock scriptBlock, SessionState sessionState)
        {
            s_setSessionState(scriptBlock, sessionState);
        }

        private static Action<ScriptBlock, SessionState> CreateSetSessionStateDelegate()
        {
            ParameterExpression scriptBlock = Expression.Parameter(typeof(ScriptBlock), nameof(scriptBlock));
            ParameterExpression sessionState = Expression.Parameter(typeof(SessionState), nameof(sessionState));

            PropertyInfo sessionStateInternal = typeof(ScriptBlock).GetProperty(
                "SessionStateInternal",
                BindingFlags.Instance | BindingFlags.NonPublic);

            PropertyInfo @internal = typeof(SessionState).GetProperty(
                "Internal",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (sessionStateInternal == null ||
                sessionStateInternal.SetMethod == null ||
                @internal == null ||
                @internal.GetMethod == null)
            {
                return CreateFailedSetSessionStateDelegate(scriptBlock, sessionState);
            }

            return Expression.Lambda<Action<ScriptBlock, SessionState>>(
                Expression.Assign(
                    Expression.Property(scriptBlock, sessionStateInternal),
                    Expression.Property(sessionState, @internal)),
                nameof(SetSessionStateInternal),
                new[] { scriptBlock, sessionState })
                .Compile();
        }

        private static Action<ScriptBlock, SessionState> CreateFailedSetSessionStateDelegate(
            ParameterExpression scriptBlock,
            ParameterExpression sessionState)
        {
            Debug.Fail("Failed to create SetSessionStateDelegate, something changed.");
            return Expression.Lambda<Action<ScriptBlock, SessionState>>(
                Expression.Empty(),
                scriptBlock,
                sessionState)
                .Compile();
        }
    }
}
