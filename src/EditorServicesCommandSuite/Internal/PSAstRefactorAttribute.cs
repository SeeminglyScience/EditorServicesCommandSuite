using System;
using EditorServicesCommandSuite.CodeGeneration.Refactors;

namespace EditorServicesCommandSuite.Internal
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PSAstRefactorAttribute : ScriptBasedRefactorProviderAttribute
    {
        public PSAstRefactorAttribute()
        {
        }

        public Type[] Targets { get; set; } = Type.EmptyTypes;

        internal override RefactorKind Kind { get; } = RefactorKind.Ast;
    }
}
