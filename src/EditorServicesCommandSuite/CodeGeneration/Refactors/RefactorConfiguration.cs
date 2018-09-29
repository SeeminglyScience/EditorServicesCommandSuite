using System;
using System.Runtime.CompilerServices;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class RefactorConfiguration
    {
        internal RefactorConfiguration()
        {
            CallSite<Action<CallSite, RefactorConfiguration>> callSite = ConfigureRefactorBinder.Get(this);
            callSite.Target(callSite, this);
        }
    }
}
