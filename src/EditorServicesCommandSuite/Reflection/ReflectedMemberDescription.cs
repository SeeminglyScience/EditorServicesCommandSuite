using System;
using System.Collections.Immutable;
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
            if (member is MethodBase methodBase)
            {
                ParameterInfo[] parameters = methodBase.GetParameters();
                var builder = ImmutableArray.CreateBuilder<ParameterDescription>(parameters.Length);
                foreach (ParameterInfo parameter in parameters)
                {
                    builder.Add(
                        new ParameterDescription(
                            parameter.Name,
                            new PSTypeName(parameter.ParameterType)));
                }

                Parameters = builder.MoveToImmutable();

                if (member is MethodInfo method)
                {
                    ReturnType = new PSTypeName(method.ReturnType);
                    IsStatic = method.IsStatic;
                    return;
                }

                if (member is ConstructorInfo constructor)
                {
                    ReturnType = new PSTypeName(constructor.ReflectedType);
                    return;
                }

                return;
            }

            Parameters = ImmutableArray<ParameterDescription>.Empty;
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

        public override ImmutableArray<ParameterDescription> Parameters { get; }

        public override PSTypeName ReturnType { get; }

        public override bool IsStatic { get; }
    }
}
