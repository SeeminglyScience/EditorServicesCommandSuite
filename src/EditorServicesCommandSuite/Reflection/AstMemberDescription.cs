using System.Collections.Generic;
using System.Linq;
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
                Parameters = function.Parameters.Select(ToParameterDescription);
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

        public override IEnumerable<ParameterDescription> Parameters { get; }

        public override PSTypeName ReturnType { get; }

        public override bool IsStatic { get; }

        private ParameterDescription ToParameterDescription(ParameterAst parameter)
        {
            return new ParameterDescription(
                parameter.Name.VariablePath.UserPath,
                parameter.Attributes.GetOutputType(typeof(object)));
        }
    }
}
