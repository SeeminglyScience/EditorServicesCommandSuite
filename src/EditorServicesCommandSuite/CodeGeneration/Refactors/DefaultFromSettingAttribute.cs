using System;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class DefaultFromSettingAttribute : Attribute
    {
        internal DefaultFromSettingAttribute(string key)
        {
            Key = key;
        }

        public string Key { get; }
    }
}
