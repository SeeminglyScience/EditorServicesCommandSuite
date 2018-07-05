using System;
using EditorServicesCommandSuite.CodeGeneration.Refactors;

namespace EditorServicesCommandSuite.Internal
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public abstract class ScriptBasedRefactorProviderAttribute : Attribute
    {
        internal ScriptBasedRefactorProviderAttribute()
        {
        }

        public virtual string Name { get; set; }

        public virtual string Description { get; set; }

        public virtual string ResourceVariable { get; set; }

        public virtual string ResourcePrefix { get; set; }

        internal abstract RefactorKind Kind { get; }
    }
}
