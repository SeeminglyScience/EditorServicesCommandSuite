using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Language
{
    internal static class AstExtensions
    {
        public static UsingDescription ToDescription(this UsingStatementAst usingStatement)
        {
            Validate.IsNotNull(nameof(usingStatement), usingStatement);

            return new UsingDescription()
            {
                Text = usingStatement.Name.Extent.Text,
                Kind = usingStatement.UsingStatementKind,
            };
        }

        public static IEnumerable<UsingDescription> ToDescriptions(
            this IEnumerable<UsingStatementAst> usingStatements)
        {
            return usingStatements.Select(ToDescription);
        }

        public static PSTypeName GetOutputType(this IEnumerable<AttributeBaseAst> source)
        {
            var typeConstraint = source.OfType<TypeConstraintAst>().FirstOrDefault();
            return typeConstraint == null ? null : new PSTypeName(typeConstraint.TypeName);
        }

        public static PSTypeName GetOutputType(this IEnumerable<AttributeBaseAst> source, Type defaultType)
        {
            return source.GetOutputType() ?? new PSTypeName(defaultType);
        }

        public static IEnumerable<Ast> Descendants(this Ast source)
        {
            return AstEnumerable.Create(source, excludeSelf: true);
        }

        public static IEnumerable<Ast> Descendants(this Ast source, Func<Ast, bool> filter)
        {
            return AstEnumerable.Create(source, filter, excludeSelf: true);
        }

        public static IEnumerable<Ast> DescendantsAndSelf(this Ast source)
        {
            return AstEnumerable.Create(source);
        }

        public static IEnumerable<Ast> DescendantsAndSelf(this Ast source, Func<Ast, bool> filter)
        {
            return AstEnumerable.Create(source, filter);
        }

        public static Ast FindAstAt(this Ast source, IScriptPosition position)
        {
            Validate.IsNotNull(nameof(position), position);

            if (source == null)
            {
                return null;
            }

            return source
                .DescendantsAndSelf(ast => ast.Extent.IsOffsetWithinOrDirectlyAfter(position.Offset))
                .OrderBySmallest()
                .FirstOrDefault();
        }

        public static Ast FindAstContaining(this Ast source, IScriptExtent extent)
        {
            Validate.IsNotNull(nameof(extent), extent);

            if (source == null)
            {
                return null;
            }

            return source
                .DescendantsAndSelf(
                    ast =>
                    {
                        return ast.Extent.ContainsOffset(extent.StartOffset)
                            && ast.Extent.IsOffsetWithinOrDirectlyAfter(extent.EndOffset);
                    })
                .OrderBySmallest()
                .FirstOrDefault();
        }

        public static IOrderedEnumerable<TAst> OrderBySmallest<TAst>(this IEnumerable<TAst> source)
            where TAst : Ast
        {
            return source
                .OrderBy(ast => ast.Extent.EndOffset - ast.Extent.StartOffset)
                .ThenByDescending(
                    ast =>
                    {
                        var count = 0;
                        for (Ast subject = ast; subject.Parent != null; subject = subject.Parent)
                        {
                            count++;
                        }

                        return count;
                    });
        }

        public static ScriptBlockAst FindRootAst(this Ast ast)
        {
            while (ast.Parent != null)
            {
                ast = ast.Parent;
            }

            return (ScriptBlockAst)ast;
        }

        public static bool TryFindParent<TAst>(this Ast ast, out TAst target)
            where TAst : Ast
        {
            target = FindParent<TAst>(ast);
            return target != null;
        }

        public static bool TryFindParent<TAst>(this Ast ast, int maxDepth, out TAst target)
            where TAst : Ast
        {
            target = FindParent<TAst>(ast, maxDepth);
            return target != null;
        }

        public static bool TryFindParent<TAst>(this Ast ast, Predicate<TAst> predicate, out TAst target)
            where TAst : Ast
        {
            target = FindParent<TAst>(ast, predicate);
            return target != null;
        }

        public static bool TryFindParent<TAst>(this Ast ast, Predicate<TAst> predicate, int maxDepth, out TAst target)
            where TAst : Ast
        {
            target = FindParent<TAst>(ast, predicate, maxDepth);
            return target != null;
        }

        public static Ast FindParent(this Ast source, Type astType)
        {
            Validate.IsNotNull(nameof(astType), astType);

            return FindParentImpl(source, astType);
        }

        public static Ast FindParent(this Ast source, Type astType, int maxDepth)
        {
            Validate.IsNotNull(nameof(astType), astType);
            Validate.IsWithinRange(nameof(maxDepth), maxDepth, 1, int.MaxValue);

            return FindParentImpl(source, astType, maxDepth: maxDepth);
        }

        public static Ast FindParent(this Ast source, Type astType, Predicate<Ast> predicate)
        {
            Validate.IsNotNull(nameof(astType), astType);
            Validate.IsNotNull(nameof(predicate), predicate);

            return FindParentImpl(source, astType, predicate);
        }

        public static Ast FindParent(this Ast source, Type astType, Predicate<Ast> predicate, int maxDepth)
        {
            Validate.IsNotNull(nameof(astType), astType);
            Validate.IsNotNull(nameof(predicate), predicate);
            Validate.IsWithinRange(nameof(maxDepth), maxDepth, 1, int.MaxValue);

            return FindParentImpl(source, astType, predicate, maxDepth);
        }

        public static TAst FindParent<TAst>(this Ast source)
            where TAst : Ast
        {
            return FindParentImpl<TAst>(source);
        }

        public static TAst FindParent<TAst>(this Ast source, int maxDepth)
            where TAst : Ast
        {
            Validate.IsWithinRange(nameof(maxDepth), maxDepth, 1, int.MaxValue);

            return FindParentImpl<TAst>(source, maxDepth: maxDepth);
        }

        public static TAst FindParent<TAst>(this Ast source, Predicate<TAst> predicate)
            where TAst : Ast
        {
            Validate.IsNotNull(nameof(predicate), predicate);

            return FindParentImpl<TAst>(source, predicate);
        }

        public static TAst FindParent<TAst>(this Ast source, Predicate<TAst> predicate, int maxDepth)
            where TAst : Ast
        {
            Validate.IsNotNull(nameof(predicate), predicate);
            Validate.IsWithinRange(nameof(maxDepth), maxDepth, 1, int.MaxValue);

            return FindParentImpl<TAst>(source, predicate, maxDepth);
        }

        public static TAst FindAst<TAst>(this Ast source)
            where TAst : Ast
        {
            return FindAstImpl<TAst>(source);
        }

        public static TAst FindAst<TAst>(this Ast source, Predicate<TAst> predicate)
            where TAst : Ast
        {
            return FindAstImpl<TAst>(source, predicate);
        }

        public static TAst FindAst<TAst>(this Ast source, bool searchNestedScriptBlocks)
            where TAst : Ast
        {
            return FindAstImpl<TAst>(source, searchNestedScriptBlocks: searchNestedScriptBlocks);
        }

        public static TAst FindAst<TAst>(
            this Ast source,
            Predicate<TAst> predicate,
            bool searchNestedScriptBlocks)
            where TAst : Ast
        {
            return FindAstImpl(source, predicate, searchNestedScriptBlocks);
        }

        public static IEnumerable<TAst> FindAllAsts<TAst>(this Ast source)
            where TAst : Ast
        {
            return FindAllAstsImpl<TAst>(source);
        }

        public static IEnumerable<TAst> FindAllAsts<TAst>(this Ast source, Predicate<TAst> predicate)
            where TAst : Ast
        {
            Validate.IsNotNull(nameof(predicate), predicate);

            return FindAllAstsImpl<TAst>(source, predicate);
        }

        public static IEnumerable<TAst> FindAllAsts<TAst>(this Ast source, bool searchNestedScriptBlocks)
            where TAst : Ast
        {
            return FindAllAstsImpl<TAst>(source, searchNestedScriptBlocks: searchNestedScriptBlocks);
        }

        public static IEnumerable<TAst> FindAllAsts<TAst>(
            this Ast source,
            Predicate<TAst> predicate,
            bool searchNestedScriptBlocks)
            where TAst : Ast
        {
            Validate.IsNotNull(nameof(predicate), predicate);

            return FindAllAstsImpl(source, predicate, searchNestedScriptBlocks);
        }

        public static IScriptPosition FindChar(this Ast ast, char target)
        {
            var index = ast.Extent.Text.IndexOf(target);
            if (index < 0)
            {
                return Empty.Position.Untitled;
            }

            return ast.Extent.StartScriptPosition.CloneWithNewOffset(
                ast.Extent.StartScriptPosition.Offset + index);
        }

        private static Ast FindParentImpl(
            this Ast source,
            Type astType,
            Predicate<Ast> predicate = null,
            int maxDepth = 3)
        {
            for (var depth = 0; source != null && maxDepth > depth; depth++)
            {
                if (astType.IsAssignableFrom(source.GetType())
                    && (predicate == null || predicate(source)))
                {
                    return source;
                }

                source = source.Parent;
            }

            return null;
        }

        private static TAst FindParentImpl<TAst>(
            this Ast source,
            Predicate<TAst> predicate = null,
            int maxDepth = 3)
            where TAst : Ast
        {
            for (var depth = 0; source != null && maxDepth > depth; depth++)
            {
                if (source is TAst targetAst && (predicate == null || predicate(targetAst)))
                {
                    return targetAst;
                }

                source = source.Parent;
            }

            return null;
        }

        private static TAst FindAstImpl<TAst>(
            this Ast source,
            Predicate<TAst> predicate = null,
            bool searchNestedScriptBlocks = false)
            where TAst : Ast
        {
            return source?.Find(
                ast => ast is TAst targetAst && (predicate == null || predicate(targetAst)),
                searchNestedScriptBlocks)
                as TAst;
        }

        private static IEnumerable<TAst> FindAllAstsImpl<TAst>(
            this Ast source,
            Predicate<TAst> predicate = null,
            bool searchNestedScriptBlocks = false)
            where TAst : Ast
        {
            return source?.FindAll(
                ast => ast is TAst targetType && (predicate == null || predicate(targetType)),
                searchNestedScriptBlocks)
                ?.Cast<TAst>();
        }
    }
}
