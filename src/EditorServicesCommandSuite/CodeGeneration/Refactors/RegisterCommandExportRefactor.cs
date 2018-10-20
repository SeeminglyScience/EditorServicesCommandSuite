using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    [Refactor(VerbsLifecycle.Register, "CommandExport")]
    internal class RegisterCommandExportRefactor : AstRefactorProvider<Ast>
    {
        private static readonly string[] s_fieldNameRanks =
        {
            "nestedmodules",
            "formatstoprocess",
            "typestoprocess",
            "scriptstoprocess",
            "requiredassemblies",
            "requiredmodules",
            "processorarchitecture",
            "clrversion",
            "dotnetframeworkversion",
            "powershellhostversion",
            "powershellhostname",
            "powershellversion",
            "description",
            "copyright",
            "companyname",
            "author",
            "guid",
            "compatiblepseditions",
            "moduleversion",
            "rootmodule",
        };

        private readonly IRefactorWorkspace _workspace;

        private readonly IRefactorUI _ui;

        internal RegisterCommandExportRefactor(IRefactorWorkspace workspace, IRefactorUI ui)
        {
            _workspace = workspace;
            _ui = ui;
        }

        public override string Name => RegisterCommandExportStrings.ProviderDisplayName;

        public override string Description => RegisterCommandExportStrings.ProviderDisplayDescription;

        internal static async Task<IEnumerable<DocumentEdit>> GetEdits(
            string functionName,
            Ast manifestAst,
            string manifestPath)
        {
            HashtableAst hashtable = manifestAst.FindAst<HashtableAst>(searchNestedScriptBlocks: true);
            Tuple<ExpressionAst, StatementAst> functionsToExport = null;
            foreach (Tuple<ExpressionAst, StatementAst> keyValuePair in hashtable.KeyValuePairs)
            {
                if (keyValuePair.Item1 is StringConstantExpressionAst stringConstant &&
                    stringConstant.Value.Equals(nameof(ManifestInfo.FunctionsToExport), StringComparison.OrdinalIgnoreCase))
                {
                    functionsToExport = keyValuePair;
                    break;
                }
            }

            var writer = new PowerShellScriptWriter(manifestAst, manifestPath);
            if (functionsToExport == null)
            {
                return await AddAsNewField(writer, hashtable, functionName);
            }

            return ManifestFieldAppender.GetEdits(
                functionsToExport.Item2,
                writer,
                functionName);
        }

        internal override bool CanRefactorTarget(DocumentContextBase request, Ast ast)
        {
            FunctionDefinitionAst parentFunction = request.RelatedAsts
                .OfType<FunctionDefinitionAst>()
                .FirstOrDefault();

            if (parentFunction == null)
            {
                return false;
            }

            ManifestInfo manifest;
            if (!ManifestInfo.TryGetWorkspaceManifest(_workspace, out manifest))
            {
                return false;
            }

            foreach (string exportedFunction in manifest.FunctionsToExport)
            {
                if (exportedFunction.Equals(parentFunction.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        internal override async Task<IEnumerable<DocumentEdit>> RequestEdits(DocumentContextBase request, Ast ast)
        {
            ManifestInfo manifestInfo;
            if (!ManifestInfo.TryGetWorkspaceManifest(_workspace, out manifestInfo))
            {
                await _ui.ShowErrorMessageOrThrowAsync(Error.ManifestRequired);
            }

            FunctionDefinitionAst function = request.RelatedAsts.OfType<FunctionDefinitionAst>().FirstOrDefault();
            if (function == null)
            {
                await _ui.ShowErrorMessageOrThrowAsync(Error.CannotFindAst, nameof(FunctionDefinitionAst));
            }

            if (Array.IndexOf(manifestInfo.FunctionsToExport, function.Name) != -1)
            {
                await _ui.ShowErrorMessageOrThrowAsync(Error.FunctionAlreadyExported, function.Name);
            }

            return await GetEdits(function.Name, manifestInfo.Ast, manifestInfo.FilePath);
        }

        private static Task<IEnumerable<DocumentEdit>> AddAsNewField(
            PowerShellScriptWriter writer,
            HashtableAst hashtable,
            string functionName)
        {
            // Find an entry that is present and is the closest to where FunctionsToExport
            // would be in a manifest created by New-ModuleManifest.
            int targetSiblingRank = int.MaxValue;
            StatementAst targetSibling = null;
            foreach (Tuple<ExpressionAst, StatementAst> pair in hashtable.KeyValuePairs)
            {
                var currentRank = Array.IndexOf(s_fieldNameRanks, GetKeyString(pair.Item1).ToLowerInvariant());
                if (currentRank != -1 && currentRank < targetSiblingRank)
                {
                    targetSibling = pair.Item2;
                    targetSiblingRank = currentRank;
                }
            }

            if (targetSibling == null)
            {
                writer.SetPosition(hashtable.Extent.StartOffset + 2);
                writer.WriteLine();
            }
            else
            {
                writer.SetPosition(targetSibling, atEnd: true);
                writer.WriteLines(2);
            }

            writer.WriteComment(ManifestStrings.FunctionsToExport, int.MaxValue);
            writer.WriteLine();
            writer.WriteHashtableEntry(
                nameof(ManifestStrings.FunctionsToExport),
                () => writer.WriteStringExpression(StringConstantType.SingleQuoted, functionName));

            writer.CreateDocumentEdits();
            return Task.FromResult(writer.Edits);
        }

        private static string GetKeyString(ExpressionAst keyAst)
        {
            if (keyAst is StringConstantExpressionAst stringConstant)
            {
                return stringConstant.Value;
            }

            return string.Empty;
        }

        private class ManifestFieldMatchingStringVisitor : AstVisitor
        {
            private readonly string _target;

            private readonly StringComparison _comparison;

            private bool _found;

            private ManifestFieldMatchingStringVisitor(string target, StringComparison comparison)
            {
                _target = target;
                _comparison = comparison;
            }

            public static bool ContainsStringConstant(
                StatementAst ast,
                string target,
                StringComparison comparison = StringComparison.OrdinalIgnoreCase)
            {
                var visitor = new ManifestFieldMatchingStringVisitor(target, comparison);
                ast.Visit(visitor);
                return visitor._found;
            }

            public override AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
            {
                if (stringConstantExpressionAst.Value.Equals(_target, _comparison))
                {
                    _found = true;
                    return AstVisitAction.StopVisit;
                }

                return AstVisitAction.Continue;
            }
        }

        private class ManifestFieldAppender : AstVisitor
        {
            private readonly PowerShellScriptWriter _writer;

            private readonly string _functionName;

            private ManifestFieldAppender(PowerShellScriptWriter writer, string functionName)
            {
                _writer = writer;
                _functionName = functionName;
            }

            public static IEnumerable<DocumentEdit> GetEdits(
                StatementAst manifestValue,
                PowerShellScriptWriter writer,
                string functionName)
            {
                if (ManifestFieldMatchingStringVisitor.ContainsStringConstant(manifestValue, functionName))
                {
                    return Enumerable.Empty<DocumentEdit>();
                }

                var visitor = new ManifestFieldAppender(writer, functionName);
                manifestValue.Visit(visitor);
                return writer.Edits;
            }

            public override AstVisitAction VisitArrayExpression(ArrayExpressionAst arrayExpressionAst)
            {
                if (arrayExpressionAst.SubExpression.Statements.Count == 1)
                {
                    return AstVisitAction.Continue;
                }

                if (arrayExpressionAst.SubExpression.Statements.Count == 0)
                {
                    _writer.SetPosition(arrayExpressionAst.Extent.StartOffset + 2);
                    _writer.WriteStringExpression(StringConstantType.SingleQuoted, _functionName);
                    _writer.CreateDocumentEdits();
                    return AstVisitAction.StopVisit;
                }

                StatementAst lastStatement = arrayExpressionAst.SubExpression.Statements.Last();
                _writer.SetPosition(lastStatement, atEnd: true);
                _writer.WriteLine();
                _writer.WriteStringExpression(StringConstantType.SingleQuoted, _functionName);
                _writer.CreateDocumentEdits();
                return AstVisitAction.StopVisit;
            }

            public override AstVisitAction VisitArrayLiteral(ArrayLiteralAst arrayLiteralAst)
            {
                // Expressions like ,'string' are still parsed as array literals.
                if (arrayLiteralAst.Elements.Count == 1)
                {
                    return AstVisitAction.Continue;
                }

                _writer.SetPosition(arrayLiteralAst, atEnd: true);
                _writer.Write(Symbols.Comma);
                if (arrayLiteralAst.Elements[0].Extent.StartLineNumber != arrayLiteralAst.Elements[1].Extent.StartLineNumber)
                {
                    _writer.WriteLine();
                }
                else
                {
                    _writer.Write(Symbols.Space);
                }

                _writer.WriteStringExpression(StringConstantType.SingleQuoted, _functionName);
                _writer.CreateDocumentEdits();
                return AstVisitAction.StopVisit;
            }

            public override AstVisitAction VisitStringConstantExpression(StringConstantExpressionAst stringConstantExpressionAst)
            {
                if (stringConstantExpressionAst.Value.Equals(_functionName, StringComparison.OrdinalIgnoreCase))
                {
                    return AstVisitAction.StopVisit;
                }

                if (stringConstantExpressionAst.Value.IndexOf(Symbols.Asterisk) != -1)
                {
                    _writer.StartWriting(stringConstantExpressionAst);
                    _writer.WriteStringExpression(StringConstantType.SingleQuoted, _functionName);
                    _writer.FinishWriting();
                    return AstVisitAction.StopVisit;
                }

                _writer.SetPosition(stringConstantExpressionAst, atEnd: true);
                _writer.WriteChars(Symbols.Comma, Symbols.Space);
                _writer.WriteStringExpression(StringConstantType.SingleQuoted, _functionName);
                _writer.CreateDocumentEdits();
                return AstVisitAction.StopVisit;
            }
        }
    }
}
