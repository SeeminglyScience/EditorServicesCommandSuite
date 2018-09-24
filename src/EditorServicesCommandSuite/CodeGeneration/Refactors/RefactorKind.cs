using System;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    /// <summary>
    /// Represents the type of language element that a refactor can target.
    /// </summary>
    [Flags]
    public enum RefactorKind
    {
        /// <summary>
        /// Indicates that the refactor provider can target AST nodes.
        /// </summary>
        Ast = 1,

        /// <summary>
        /// Indicates that the refactor provider can target PowerShell language tokens.
        /// </summary>
        Token = 2,

        /// <summary>
        /// Indicates that the refactor provider can target the text selected within the
        /// host editor.
        /// </summary>
        Selection = 4,
    }
}
