using System;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class SettingAttribute : Attribute
    {
        internal SettingAttribute(string key)
        {
            Key = key;
        }

        public string Key { get; }

        public string Default { get; set; }
    }
}
