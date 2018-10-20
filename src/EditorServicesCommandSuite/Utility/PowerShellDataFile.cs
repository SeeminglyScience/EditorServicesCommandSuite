using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.Utility
{
    internal abstract class PowerShellDataFile
    {
        private bool _needsInitialization = true;

        private Dictionary<string, ExpressionAst> _values = new Dictionary<string, ExpressionAst>(StringComparer.OrdinalIgnoreCase);

        private FileSystemWatcher _watcher;

        protected PowerShellDataFile(string resolvedPath)
        {
            FilePath = resolvedPath;
        }

        internal string FilePath { get; }

        internal Ast Ast { get; private set; }

        internal static TSchema GetOrCreate<TSchema>(string path, Func<string, TSchema> createFactory)
            where TSchema : PowerShellDataFile
        {
            return FileCache<TSchema>.Instance.GetOrAdd(
                path,
                createFactory);
        }

        internal void EnsureInitialized()
        {
            if (!_needsInitialization)
            {
                return;
            }

            if (_watcher == null)
            {
                _watcher = CreateWatcher();
            }

            if (!File.Exists(FilePath))
            {
                Ast = Empty.ScriptAst.Get(FilePath);
                _needsInitialization = false;
                return;
            }

            Ast = Parser.ParseFile(FilePath, out _, out ParseError[] errors);
            if (errors != null && errors.Length > 0)
            {
                _needsInitialization = false;
                return;
            }

            HashtableAst hashtable = Ast.FindAst<HashtableAst>(searchNestedScriptBlocks: true);
            if (hashtable == null)
            {
                _needsInitialization = false;
                return;
            }

            foreach (Tuple<ExpressionAst, StatementAst> pair in hashtable.KeyValuePairs)
            {
                var constant = pair.Item1 as StringConstantExpressionAst;
                if (constant == null)
                {
                    continue;
                }

                var pipeline = pair.Item2 as PipelineAst;
                if (pipeline == null || pipeline.PipelineElements.Count != 1)
                {
                    _values.Remove(constant.Value);
                    continue;
                }

                var commandExpression = pipeline.PipelineElements[0] as CommandExpressionAst;
                if (commandExpression == null)
                {
                    _values.Remove(constant.Value);
                    continue;
                }

                if (_values.ContainsKey(constant.Value))
                {
                    _values[constant.Value] = commandExpression.Expression;
                }
                else
                {
                    _values.Add(constant.Value, commandExpression.Expression);
                }
            }

            _needsInitialization = false;
        }

        protected TResult[] GetArrayField<TResult>(string fieldName)
        {
            EnsureInitialized();
            ExpressionAst expression;
            if (!_values.TryGetValue(fieldName, out expression))
            {
                return Array.Empty<TResult>();
            }

            return UnwrapArrayExpression<TResult>(expression);
        }

        protected TResult GetField<TResult>(string fieldName)
        {
            EnsureInitialized();
            ExpressionAst expression;
            if (!_values.TryGetValue(fieldName, out expression))
            {
                return default(TResult);
            }

            return UnwrapExpression<TResult>(expression);
        }

        private static TResult[] UnwrapArrayExpression<TResult>(ExpressionAst expression)
        {
            if (expression is ParenExpressionAst paren)
            {
                return UnwrapArrayExpression<TResult>(paren.Pipeline.GetPureExpression());
            }

            TResult[] result;
            if (expression is ArrayLiteralAst arrayLiteral)
            {
                result = new TResult[arrayLiteral.Elements.Count];
                for (var i = 0; i < arrayLiteral.Elements.Count; i++)
                {
                    result[i] = UnwrapExpression<TResult>(arrayLiteral.Elements[i]);
                }

                return result;
            }

            ReadOnlyCollection<StatementAst> statements = null;
            if (expression is SubExpressionAst sub)
            {
                statements = sub.SubExpression.Statements;
            }

            if (expression is ArrayExpressionAst arrayExpression)
            {
                statements = arrayExpression.SubExpression.Statements;
            }

            if (statements == null)
            {
                return new[] { UnwrapExpression<TResult>(expression) };
            }

            if (statements.Count == 0)
            {
                return Array.Empty<TResult>();
            }

            if (statements.Count == 1)
            {
                return UnwrapArrayExpression<TResult>(statements[0].FindAst<ExpressionAst>());
            }

            result = new TResult[statements.Count];
            for (var i = 0; i < statements.Count; i++)
            {
                result[i] = UnwrapExpression<TResult>(statements[i].FindAst<ExpressionAst>());
            }

            return result;
        }

        private static TResult UnwrapExpression<TResult>(ExpressionAst expression)
        {
            TResult result;

            if (expression is ConstantExpressionAst constant)
            {
                LanguagePrimitives.TryConvertTo(
                    constant.Value,
                    System.Globalization.CultureInfo.CurrentCulture,
                    out result);

                return result;
            }

            try
            {
                LanguagePrimitives.TryConvertTo(
                    expression.SafeGetValue(),
                    System.Globalization.CultureInfo.CurrentCulture,
                    out result);

                return result;
            }
            catch (InvalidOperationException)
            {
                // IOE will be thrown if the expression cannot safely be evaluated like if it contains
                // a variable.
                return default(TResult);
            }
        }

        private FileSystemWatcher CreateWatcher()
        {
            var watcher = new FileSystemWatcher(
                Path.GetDirectoryName(FilePath),
                Path.GetFileName(FilePath));

            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Changed += OnManifestModified;
            watcher.Created += OnManifestModified;
            watcher.Deleted += OnManifestModified;
            watcher.Renamed += OnManifestModified;
            watcher.EnableRaisingEvents = true;

            return watcher;
        }

        private void OnManifestModified(object source, EventArgs eventArgs)
        {
            _needsInitialization = true;
        }

        private static class FileCache<TSchema>
            where TSchema : PowerShellDataFile
        {
            internal static ConcurrentDictionary<string, TSchema> Instance =
                new ConcurrentDictionary<string, TSchema>(PathUtils.PathComparer);
        }
    }
}
