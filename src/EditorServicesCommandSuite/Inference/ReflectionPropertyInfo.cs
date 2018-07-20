using System;
using System.Management.Automation;

namespace EditorServicesCommandSuite.Inference
{
    internal class ReflectionPropertyInfo : PSPropertyInfo
    {
        private readonly System.Reflection.PropertyInfo _property;

        internal ReflectionPropertyInfo(System.Reflection.PropertyInfo property)
        {
            _property = property;
            SetMemberName(_property.Name);
        }

        public override bool IsGettable => _property.GetGetMethod()?.IsPublic ?? false;

        public override bool IsSettable => _property.GetSetMethod()?.IsPublic ?? false;

        public override PSMemberTypes MemberType => PSMemberTypes.Property;

        public override string TypeNameOfValue => _property.PropertyType.FullName;

        public override object Value
        {
            get => throw new System.NotSupportedException();
            set => throw new System.NotSupportedException();
        }

        internal Type ReturnType => _property.PropertyType;

        public override PSMemberInfo Copy()
        {
            return new ReflectionPropertyInfo(_property);
        }
    }
}
