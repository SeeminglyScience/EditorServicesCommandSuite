using System;
using System.Management.Automation;
using System.Reflection;

namespace EditorServicesCommandSuite.Reflection
{
    internal static class ReflectionCache
    {
        internal static Type PositionHelper;

        internal static Type InternalScriptExtent;

        internal static ConstructorInfo InternalScriptExtent_Ctor;

        internal static PropertyInfo InternalScriptExtent_PositionHelper;

        internal static Type InternalScriptPosition;

        internal static MethodInfo InternalScriptPosition_CloneWithNewOffset;

        internal static PropertyInfo ExpandableStringExpressionAst_FormatExpression;

        private static readonly BindingFlags s_instance =
            BindingFlags.NonPublic | BindingFlags.Instance;

        static ReflectionCache()
        {
            Initialize();
        }

        internal static void Initialize()
        {
            PositionHelper =
                typeof(PSObject)
                    .Assembly
                    .GetType("System.Management.Automation.Language.PositionHelper");

            InternalScriptExtent =
                typeof(PSObject)
                    .Assembly
                    .GetType("System.Management.Automation.Language.InternalScriptExtent");

            InternalScriptExtent_Ctor =
                InternalScriptExtent
                    .GetConstructor(
                        s_instance,
                        null,
                        new[] { PositionHelper, typeof(int), typeof(int) },
                        new[] { new ParameterModifier(3) });

            InternalScriptExtent_PositionHelper =
                InternalScriptExtent.GetProperty("PositionHelper", s_instance);

            InternalScriptPosition =
                typeof(PSObject)
                    .Assembly
                    .GetType("System.Management.Automation.Language.InternalScriptPosition");

            InternalScriptPosition_CloneWithNewOffset =
                InternalScriptPosition
                    .GetMethod(
                        "CloneWithNewOffset",
                        s_instance,
                        null,
                        new[] { typeof(int) },
                        new[] { new ParameterModifier(1) });

            ExpandableStringExpressionAst_FormatExpression =
                typeof(System.Management.Automation.Language.ExpandableStringExpressionAst)
                    .GetProperty(
                        "FormatExpression",
                        s_instance);
        }
    }
}
