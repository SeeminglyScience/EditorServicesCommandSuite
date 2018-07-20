using System;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace EditorServicesCommandSuite.Inference
{
    internal class ReflectionMethodInfo : PSMethodInfo
    {
        private readonly System.Reflection.MethodBase _method;

        internal ReflectionMethodInfo(System.Reflection.MethodBase method)
        {
            if (_method is System.Reflection.MethodInfo methodInfo)
            {
                ReturnType = methodInfo.ReturnType;
            }
            else
            {
                ReturnType = method.ReflectedType;
            }

            _method = method;
            SetMemberName(method.IsConstructor ? "new" : method.Name);
        }

        public override Collection<string> OverloadDefinitions => throw new System.NotSupportedException();

        public override PSMemberTypes MemberType => PSMemberTypes.Method;

        public override string TypeNameOfValue => ReturnType.FullName;

        internal Type ReturnType { get; }

        public override object Invoke(params object[] arguments)
        {
            throw new System.NotSupportedException();
        }

        public override PSMemberInfo Copy()
        {
            return new ReflectionMethodInfo(_method);
        }
    }
}
