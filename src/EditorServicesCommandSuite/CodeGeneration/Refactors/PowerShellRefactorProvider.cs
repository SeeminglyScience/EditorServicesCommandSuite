using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal class PowerShellRefactorProvider : IDocumentRefactorProvider
    {
        private const string NameResourceSuffix = "Name";

        private const string DescriptionResourceSuffix = "Description";

        private const string TargetParameterName = "RefactorTarget";

        private const string TestParameterName = "Test";

        private readonly IPowerShellExecutor _executor;

        private readonly FunctionInfo _function;

        private RefactorKind _kind;

        private string _name;

        private string _description;

        private Type _targetType;

        internal PowerShellRefactorProvider(
            IPowerShellExecutor executor,
            FunctionInfo function)
        {
            _executor = executor;
            _function = function;
            Initialize();
        }

        public string Id => _function.Name;

        public string Name => _name;

        public string Description => _description;

        public RefactorKind Kind => _kind;

        public bool CanRefactorTarget(DocumentContextBase request)
        {
            return CanRefactorTarget(
                request,
                GetTargetByKind(Kind, request));
        }

        public async Task<IEnumerable<DocumentEdit>> RequestEdits(DocumentContextBase request)
        {
            using (var pwsh = PowerShell.Create())
            {
                pwsh.AddCommand(_function)
                    .AddParameter(TargetParameterName, GetTargetByKind(Kind, request));

                await _executor.ExecuteCommandAsync<bool>(pwsh.Commands.Clone());

                return Enumerable.Empty<DocumentEdit>();
            }
        }

        public bool TryGetRefactorInfo(DocumentContextBase request, out IRefactorInfo info)
        {
            var target = GetTargetByKind(Kind, request);
            if (target == null)
            {
                info = null;
                return false;
            }

            if (!_targetType.IsAssignableFrom(target.GetType()) ||
                !CanRefactorTarget(request, target))
            {
                info = null;
                return false;
            }

            info = new PowerShellRefactorInfo(this, request);
            return true;
        }

        private static Type GetTargetType(FunctionInfo function)
        {
            var targetParam = function.ScriptBlock.Ast
                .FindAst<ParamBlockAst>(searchNestedScriptBlocks: true)
                .Parameters
                .FirstOrDefault(p =>
                {
                    return p.Name.VariablePath.UserPath
                        .Equals(TargetParameterName, StringComparison.OrdinalIgnoreCase);
                });

            return targetParam.Attributes
                .OfType<TypeConstraintAst>()
                .FirstOrDefault()
                .TypeName
                .GetReflectionType();
        }

        private void Initialize()
        {
            var attribute = _function.ScriptBlock.Attributes
                .OfType<ScriptBasedRefactorProviderAttribute>()
                .FirstOrDefault();

            if (attribute == null)
            {
                throw new InvalidOperationException();
            }

            _kind = attribute.Kind;

            if (!string.IsNullOrWhiteSpace(attribute.Name))
            {
                _name = attribute.Name;
                _description = attribute.Description;
                return;
            }

            if (string.IsNullOrWhiteSpace(attribute.ResourceVariable))
            {
                throw new InvalidOperationException();
            }

            var resources = (_function?.Module.SessionState.PSVariable
                .Get(attribute.ResourceVariable).Value as PSObject)?.BaseObject as Hashtable;

            if (resources == null)
            {
                throw new InvalidOperationException();
            }

            _name = resources[attribute.ResourcePrefix + NameResourceSuffix]?.ToString();

            _description = resources[attribute.ResourcePrefix + DescriptionResourceSuffix]?.ToString();

            _targetType = GetTargetType(_function);
        }

        private bool CanRefactorTarget(DocumentContextBase request, object target)
        {
            if (Kind == RefactorKind.Selection &&
                request.SelectionExtent.StartOffset == request.SelectionExtent.EndOffset)
            {
                return false;
            }

            using (var pwsh = PowerShell.Create())
            {
                pwsh.AddCommand(_function)
                    .AddParameter(TestParameterName, true)
                    .AddParameter(TargetParameterName, target);

                return _executor
                    .ExecuteCommand<bool>(
                        pwsh.Commands.Clone(),
                        request.CancellationToken)
                    .FirstOrDefault();
            }
        }

        private object GetTargetByKind(RefactorKind kind, DocumentContextBase request)
        {
            if (kind == RefactorKind.Ast)
            {
                if (_targetType.IsAssignableFrom(request.Ast.GetType()))
                {
                    return request.Ast;
                }

                return request.Ast.FindParent(_targetType) ?? request.Ast;
            }

            if (kind == RefactorKind.Token)
            {
                return request.Token?.Value;
            }

            return request.SelectionExtent;
        }

        private class PowerShellRefactorInfo : IRefactorInfo
        {
            private readonly PowerShellRefactorProvider _provider;

            private readonly DocumentContextBase _context;

            internal PowerShellRefactorInfo(PowerShellRefactorProvider provider, DocumentContextBase context)
            {
                _provider = provider;
                _context = context;
            }

            public string Name => _provider.Name;

            public string Description => _provider.Description;

            public IDocumentRefactorProvider Provider => _provider;

            public async Task<IEnumerable<DocumentEdit>> GetDocumentEdits()
            {
                return await _provider.RequestEdits(_context);
            }
        }
    }
}
