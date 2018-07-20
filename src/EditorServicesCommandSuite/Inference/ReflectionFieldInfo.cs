using System;
using System.Management.Automation;

namespace EditorServicesCommandSuite.Inference
{
    internal class ReflectionFieldInfo : PSPropertyInfo
    {
        private readonly System.Reflection.FieldInfo _field;

        internal ReflectionFieldInfo(System.Reflection.FieldInfo field)
        {
            _field = field;
            SetMemberName(field.Name);
        }

        public override bool IsGettable { get; } = true;

        public override bool IsSettable => !(_field.IsLiteral || _field.IsInitOnly);

        public override PSMemberTypes MemberType => PSMemberTypes.Property;

        public override string TypeNameOfValue => _field.FieldType.FullName;

        public override object Value
        {
            get => throw new System.NotSupportedException();
            set => throw new System.NotSupportedException();
        }

        internal Type ReturnType => _field.FieldType;

        public override PSMemberInfo Copy()
        {
            return new ReflectionFieldInfo(_field);
        }
    }
}
