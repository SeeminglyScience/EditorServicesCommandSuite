using System;
using EditorServicesCommandSuite.CodeGeneration.Refactors;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Represents a function based refactor provider that targets PowerShell token.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PSTokenRefactorAttribute : ScriptBasedRefactorProviderAttribute
    {
        internal override RefactorKind Kind { get; } = RefactorKind.Token;
    }
}
