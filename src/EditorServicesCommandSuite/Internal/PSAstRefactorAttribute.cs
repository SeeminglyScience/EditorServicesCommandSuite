using System;
using EditorServicesCommandSuite.CodeGeneration.Refactors;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Represents a function based refactor provider that targets an AST.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class PSAstRefactorAttribute : ScriptBasedRefactorProviderAttribute
    {
        /// <summary>
        /// Gets or sets the AST types that the function can accept.
        /// </summary>
        public Type[] Targets { get; set; } = Type.EmptyTypes;

        internal override RefactorKind Kind { get; } = RefactorKind.Ast;
    }
}
