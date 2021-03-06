using System;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal class DefaultFromSettingAttribute : SettingAttribute
    {
        internal DefaultFromSettingAttribute(string key)
            : base(key)
        {
        }
    }
}
