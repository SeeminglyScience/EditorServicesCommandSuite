using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Reflection;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Reflection;
using Xunit;

namespace EditorServicesCommandSuite.Tests
{
    public class ImplementAbstractMethodsTests
    {
        [Fact]
        public async void AddsASingleMethod()
        {
            Assert.Equal(
                "using namespace System\n\n" +
                "class test : test {\n" +
                "\t[void] TestMethod() {\n" +
                "\t\tthrow [NotImplementedException]::new()\n" +
                "\t}\n" +
                "}",
                await GetRefactoredTextAsync(
                    "class test : te{{c}}st {\n}",
                    MemberOfType.Method().Named("TestMethod")));
        }

        [Fact]
        public async void AddsASingleProperty()
        {
            Assert.Equal(
                "class test : test {\n" +
                "\t[string] $Name;\n" +
                "}",
                await GetRefactoredTextAsync(
                    "class test : te{{c}}st {\n}",
                    MemberOfType.Property().Named("Name").Returns(typeof(string))));
        }

        [Fact]
        public async void AppendsAfterExistingMembers()
        {
            Assert.Equal(
                "using namespace System\n\n" +
                "class test : test {\n" +
                "\t[void] TestMethod1() {}\n\n" +
                "\t[void] TestMethod2() {\n" +
                "\t\tthrow [NotImplementedException]::new()\n" +
                "\t}\n" +
                "}",
                await GetRefactoredTextAsync(
                    "class test : te{{c}}st {\n\t[void] TestMethod1() {}\n}",
                    MemberOfType.Method().Named("TestMethod2")));
        }

        private async Task<string> GetRefactoredTextAsync(
            string testString,
            params MemberDescription[] implementableMembers)
        {
            return await MockContext.GetRefactoredTextAsync(
                testString,
                context => Task.FromResult(
                    ImplementAbstractMethodsRefactor.GetEdits(
                        context.Ast.FindParent<TypeDefinitionAst>(),
                        context.Ast.FindParent<TypeConstraintAst>(),
                        context.Token,
                        implementableMembers)));
        }

        private class MemberOfType : MemberDescription
        {
            private string _name;

            private MemberTypes? _memberType;

            private List<ParameterDescription> _parameters = new List<ParameterDescription>();

            private PSTypeName _returnType;

            private bool _isStatic;

            public override string Name => _name;

            public override MemberTypes MemberType => _memberType.GetValueOrDefault();

            public override IEnumerable<ParameterDescription> Parameters => _parameters;

            public override PSTypeName ReturnType => _returnType ?? new PSTypeName(typeof(void));

            public override bool IsStatic => _isStatic;

            internal static MemberOfType Method()
            {
                return new MemberOfType() { _memberType = MemberTypes.Method };
            }

            internal static MemberOfType Property()
            {
                return new MemberOfType() { _memberType = MemberTypes.Property };
            }

            internal MemberOfType Static()
            {
                _isStatic = true;
                return this;
            }

            internal MemberOfType Named(string name)
            {
                _name = name;
                return this;
            }

            internal MemberOfType Receives(Dictionary<string, PSTypeName> parameters)
            {
                foreach (var pair in parameters)
                {
                    _parameters.Add(new ParameterDescription(pair.Key, pair.Value));
                }

                return this;
            }

            internal MemberOfType Receives(string name, PSTypeName parameterType)
            {
                _parameters.Add(new ParameterDescription(name, parameterType));
                return this;
            }

            internal MemberOfType Receives(string name, System.Type parameterType)
            {
                _parameters.Add(new ParameterDescription(name, new PSTypeName(parameterType)));
                return this;
            }

            internal MemberOfType Returns(PSTypeName returnType)
            {
                _returnType = returnType;
                return this;
            }

            internal MemberOfType Returns(System.Type returnType)
            {
                _returnType = new PSTypeName(returnType);
                return this;
            }
        }
    }
}
