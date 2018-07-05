using System.Reflection;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class RefactorConfiguration
    {
        internal RefactorConfiguration()
        {
            var propertiesFromSettings = this.GetType().FindMembers(
                MemberTypes.Property,
                BindingFlags.Public | BindingFlags.Instance,
                IsConfigurableFromSettings,
                null);

            foreach (PropertyInfo property in propertiesFromSettings)
            {
                var keyName = property.GetCustomAttribute<DefaultFromSettingAttribute>().Key;
                if (Settings.TryGetSetting(
                    keyName,
                    property.PropertyType,
                    out object settingValue))
                {
                    property.SetValue(this, settingValue);
                }
            }
        }

        private bool IsConfigurableFromSettings(MemberInfo m, object filterCriteria)
        {
            return m.IsDefined(typeof(DefaultFromSettingAttribute));
        }
    }
}
