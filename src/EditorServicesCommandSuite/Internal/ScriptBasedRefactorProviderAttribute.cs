using System;
using EditorServicesCommandSuite.CodeGeneration.Refactors;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Represents a function based refactor provider.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public abstract class ScriptBasedRefactorProviderAttribute : Attribute
    {
        internal ScriptBasedRefactorProviderAttribute()
        {
        }

        /// <summary>
        /// Gets or sets the name of the refactor provider.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the refactor provider.
        /// </summary>
        public virtual string Description { get; set; }

        /// <summary>
        /// Gets or sets the name of the variable that contains resource strings.
        /// </summary>
        public virtual string ResourceVariable { get; set; }

        /// <summary>
        /// Gets or sets the prefix of the resource name.
        /// </summary>
        public virtual string ResourcePrefix { get; set; }

        internal abstract RefactorKind Kind { get; }
    }
}
