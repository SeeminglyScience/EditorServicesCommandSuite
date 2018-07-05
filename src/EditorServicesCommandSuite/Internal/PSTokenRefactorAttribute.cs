using System;
using EditorServicesCommandSuite.CodeGeneration.Refactors;

namespace EditorServicesCommandSuite.Internal
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PSTokenRefactorAttribute : ScriptBasedRefactorProviderAttribute
    {
        internal override RefactorKind Kind { get; } = RefactorKind.Token;
    }
}
