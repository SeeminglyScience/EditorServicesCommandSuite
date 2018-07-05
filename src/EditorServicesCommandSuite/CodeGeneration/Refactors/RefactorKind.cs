using System;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Flags]
    internal enum RefactorKind
    {
        Ast = 1,

        Token = 2,

        Selection = 4,
    }
}
