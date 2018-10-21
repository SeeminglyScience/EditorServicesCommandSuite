using System.Collections.Immutable;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.Reflection
{
    internal class AstMemberDescription : MemberDescription
    {
        internal AstMemberDescription(MemberAst member)
            : base()
        {
            Name = member.Name;
            if (member is FunctionMemberAst function)
            {
                var builder = ImmutableArray.CreateBuilder<ParameterDescription>(function.Parameters.Count);
                foreach (ParameterAst parameter in function.Parameters)
                {
                    builder.Add(
                        new ParameterDescription(
                            parameter.Name.VariablePath.UserPath,
                            parameter.Attributes.GetOutputType()));
                }

                Parameters = builder.MoveToImmutable();
                IsStatic = function.IsStatic;
                if (function.IsConstructor)
                {
                    MemberType = MemberTypes.Constructor;
                    ReturnType = new PSTypeName((TypeDefinitionAst)function.Parent);
                    return;
                }

                MemberType = MemberTypes.Method;
                ReturnType = new PSTypeName(function.ReturnType.TypeName);
                return;
            }

            PropertyMemberAst property = (PropertyMemberAst)member;
            IsStatic = property.IsStatic;
            ReturnType = property.Attributes.GetOutputType(typeof(object));
        }

        public override string Name { get; }

        public override MemberTypes MemberType { get; }

        public override ImmutableArray<ParameterDescription> Parameters { get; }

        public override PSTypeName ReturnType { get; }

        public override bool IsStatic { get; }
    }
}
