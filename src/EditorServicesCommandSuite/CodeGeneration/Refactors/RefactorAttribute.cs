using System;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class RefactorAttribute : Attribute
    {
        internal RefactorAttribute(string verb, string noun)
        {
            Verb = verb;
            Noun = noun;
        }

        public string Verb { get; }

        public string Noun { get; }

        public Type Parameters { get; set; }
    }
}
