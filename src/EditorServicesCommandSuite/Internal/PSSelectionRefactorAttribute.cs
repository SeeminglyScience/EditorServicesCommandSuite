using System;
using EditorServicesCommandSuite.CodeGeneration.Refactors;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Represents a function based refactor provider that targets a script extent.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PSSelectionRefactorAttribute : ScriptBasedRefactorProviderAttribute
    {
        internal override RefactorKind Kind { get; } = RefactorKind.Selection;
    }
}
