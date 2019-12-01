using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Reflection;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsData.Expand, "TypeImplementation")]
    internal class ImplementAbstractMethodsRefactor : RefactorProvider
    {
        internal ImplementAbstractMethodsRefactor(IRefactorUI ui)
        {
            UI = ui;
        }

        public override string Name { get; } = ImplementAbstractMethodsStrings.ProviderDisplayName;

        public override string Description { get; } = ImplementAbstractMethodsStrings.ProviderDisplayDescription;

        public override ImmutableArray<CodeAction> SupportedActions { get; }
            = ImmutableArray.Create(
                CodeAction.Inactive(CodeActionIds.ImplementVirtualMethods, "Implement Abstract Class"),
                CodeAction.Inactive(CodeActionIds.ImplementVirtualMethods, "Generate overrides..."));

        internal IRefactorUI UI { get; }

        private CodeAction AbstractCodeAction => SupportedActions[0];

        private CodeAction OverridesCodeAction => SupportedActions[1];

        public override async Task ComputeCodeActions(DocumentContextBase context)
        {
            if (!(context.Ast is TypeConstraintAst ast))
            {
                return;
            }

            if (!(ast.Parent is TypeDefinitionAst typeDefinition))
            {
                return;
            }

            Type type = ast.TypeName.GetReflectionType();
            if (type == null)
            {
                return;
            }

            // Needs optimization.
            var virtualMethods = MemberUtil.GetVirtualMethods(type, abstractOnly: false)
                .Except(typeDefinition.Members.ToMemberDescriptions())
                .ToImmutableArray();

            if (virtualMethods.Length > 0)
            {
                await context.RegisterCodeActionAsync(
                    OverridesCodeAction.With(AddMethodImplementations, (virtualMethods, typeDefinition)))
                    .ConfigureAwait(false);
            }

            var abstractMethods = MemberUtil.GetVirtualMethods(type, abstractOnly: true)
                .Except(typeDefinition.Members.ToMemberDescriptions())
                .ToImmutableArray();

            if (abstractMethods.Length > 0)
            {
                await context.RegisterCodeActionAsync(
                    AbstractCodeAction.With(AddMethodImplementations, (abstractMethods, typeDefinition)))
                    .ConfigureAwait(false);
            }
        }

        internal static IEnumerable<DocumentEdit> GetEdits(
            TypeDefinitionAst parentTypeDefinition,
            TypeConstraintAst typeConstraint,
            LinkedListNode<Token> currentToken,
            IEnumerable<MemberDescription> implementableMembers)
        {
            var writer = new PowerShellScriptWriter(typeConstraint);
            var lastMember = parentTypeDefinition.Members.LastOrDefault();
            if (lastMember != null)
            {
                writer.SetPosition(lastMember, atEnd: true);
                writer.WriteLine();
                writer.WriteLine();
            }
            else
            {
                writer.SetPosition(
                    currentToken
                        .FindNext(token => token.Value.Kind == TokenKind.LCurly)
                        .Value
                        .Extent,
                    atEnd: true);
                writer.FrameOpen();
            }

            writer.WriteEachWithSeparator(
                implementableMembers.ToArray(),
                member => writer.WriteMemberDefinition(member),
                () => writer.WriteLines(amount: 2));

            writer.CreateDocumentEdits();
            return writer.Edits;
        }

        internal static async Task AddMethodImplementations(
            DocumentContextBase context,
            ImmutableArray<MemberDescription> virtualMethods,
            TypeDefinitionAst typeDefinition)
        {
            IEnumerable<DocumentEdit> edits = GetEdits(
                typeDefinition,
                (TypeConstraintAst)context.Ast,
                context.Token,
                virtualMethods);

            await context.RegisterWorkspaceChangeAsync(
                WorkspaceChange.EditDocument(
                    context.Document,
                    edits))
                .ConfigureAwait(false);
        }
    }
}
