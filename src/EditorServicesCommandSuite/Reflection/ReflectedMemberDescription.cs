using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace EditorServicesCommandSuite.Reflection
{
    internal class ReflectedMemberDescription : MemberDescription
    {
        private readonly MemberInfo _member;

        internal ReflectedMemberDescription(MemberInfo member)
            : base()
        {
            _member = member ?? throw new ArgumentNullException(nameof(member));
            if (member is MethodInfo method)
            {
                ReturnType = new PSTypeName(method.ReturnType);
                Parameters = method.GetParameters().Select(ToParameterDescription);
                IsStatic = method.IsStatic;
                return;
            }

            if (member is ConstructorInfo constructor)
            {
                ReturnType = new PSTypeName(constructor.ReflectedType);
                Parameters = constructor.GetParameters().Select(ToParameterDescription);
                return;
            }

            Parameters = Enumerable.Empty<ParameterDescription>();
            if (member is PropertyInfo property)
            {
                ReturnType = new PSTypeName(property.PropertyType);
                IsStatic = property.GetMethod.IsStatic;
                return;
            }

            if (member is FieldInfo field)
            {
                ReturnType = new PSTypeName(field.FieldType);
                IsStatic = field.IsStatic;
                return;
            }

            if (member is EventInfo eventInfo)
            {
                IsStatic = eventInfo.AddMethod.IsStatic;
            }

            ReturnType = new PSTypeName(typeof(void));
        }

        public override string Name => _member.Name;

        public override MemberTypes MemberType => _member.MemberType;

        public override IEnumerable<ParameterDescription> Parameters { get; }

        public override PSTypeName ReturnType { get; }

        public override bool IsStatic { get; }

        private ParameterDescription ToParameterDescription(ParameterInfo parameter)
        {
            return new ParameterDescription(
                parameter.Name,
                new PSTypeName(parameter.ParameterType));
        }
    }
}
