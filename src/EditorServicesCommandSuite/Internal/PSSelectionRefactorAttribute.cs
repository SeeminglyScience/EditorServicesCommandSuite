using System;
using EditorServicesCommandSuite.CodeGeneration.Refactors;

namespace EditorServicesCommandSuite.Internal
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PSSelectionRefactorAttribute : ScriptBasedRefactorProviderAttribute
    {
        internal override RefactorKind Kind { get; } = RefactorKind.Selection;
    }
}
