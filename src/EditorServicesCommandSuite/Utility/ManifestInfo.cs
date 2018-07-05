using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.Utility
{
    internal class ManifestInfo
    {
        private static readonly ConcurrentDictionary<string, ManifestInfo> s_manifestCache =
            new ConcurrentDictionary<string, ManifestInfo>();

        private readonly string _path;

        private bool _needsInitialization = true;

        private Dictionary<string, ExpressionAst> _values = new Dictionary<string, ExpressionAst>();

        private FileSystemWatcher _watcher;

        private ManifestInfo(string resolvedPath)
        {
            _path = resolvedPath;
        }

        internal string ModuleName => Path.GetFileNameWithoutExtension(_path);

        internal string RootModule => GetField<string>(nameof(RootModule));

        internal Version ModuleVersion => GetField<Version>(nameof(ModuleVersion));

        internal string Author => GetField<string>(nameof(Author));

        internal string CompanyName => GetField<string>(nameof(CompanyName));

        internal string Copyright => GetField<string>(nameof(Copyright));

        internal string Description => GetField<string>(nameof(Description));

        internal string PowerShellVersion => GetField<string>(nameof(PowerShellVersion));

        internal string DotNetFrameworkVersion => GetField<string>(nameof(DotNetFrameworkVersion));

        internal string CLRVersion => GetField<string>(nameof(CLRVersion));

        internal string ProcessorArchitecture => GetField<string>(nameof(ProcessorArchitecture));

        internal string RequiredModules => GetField<string>(nameof(RequiredModules));

        internal string[] FunctionsToExport => GetArrayField<string>(nameof(FunctionsToExport));

        internal string[] CmdletsToExport => GetArrayField<string>(nameof(CmdletsToExport));

        internal string[] VariablesToExport => GetArrayField<string>(nameof(VariablesToExport));

        internal string[] AliasesToExport => GetArrayField<string>(nameof(AliasesToExport));

        internal string[] FileList => GetArrayField<string>(nameof(FileList));

        internal System.Collections.Hashtable PrivateData => GetField<System.Collections.Hashtable>(nameof(PrivateData));

        internal static bool TryGetWorkspaceManifest(IRefactorWorkspace workspace, out ManifestInfo manifestInfo)
        {
            if (workspace == null)
            {
                manifestInfo = null;
                return false;
            }

            string sourcePath = Settings.SourceManifestPath;
            if (string.IsNullOrEmpty(sourcePath))
            {
                manifestInfo = null;
                return false;
            }

            workspace.TryResolveRelativePath(sourcePath, out string manifestPath);
            if (string.IsNullOrEmpty(manifestPath))
            {
                manifestInfo = null;
                return false;
            }

            manifestInfo = s_manifestCache.GetOrAdd(
                manifestPath,
                path => new ManifestInfo(manifestPath));

            return true;
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
                return Empty<TResult>.Array;
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

        private void MaybeInitialize()
        {
            if (!_needsInitialization)
            {
                return;
            }

            if (_watcher == null)
            {
                _watcher = CreateWatcher();
            }

            if (!File.Exists(_path))
            {
                _needsInitialization = false;
                return;
            }

            Ast ast = Parser.ParseFile(_path, out _, out ParseError[] errors);
            if (errors != null && errors.Length > 0)
            {
                _needsInitialization = false;
                return;
            }

            HashtableAst hashtable = ast.FindAst<HashtableAst>(searchNestedScriptBlocks: true);
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
                    continue;
                }

                var commandExpression = pipeline.PipelineElements[0] as CommandExpressionAst;
                if (commandExpression == null)
                {
                    continue;
                }

                _values.Add(constant.Value, commandExpression.Expression);
            }

            _needsInitialization = false;
        }

        private FileSystemWatcher CreateWatcher()
        {
            var watcher = new FileSystemWatcher(
                Path.GetDirectoryName(_path),
                Path.GetFileName(_path));

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

        private TResult[] GetArrayField<TResult>(string fieldName)
        {
            MaybeInitialize();
            ExpressionAst expression;
            if (!_values.TryGetValue(fieldName, out expression))
            {
                return Empty<TResult>.Array;
            }

            return UnwrapArrayExpression<TResult>(expression);
        }

        private TResult GetField<TResult>(string fieldName)
        {
            MaybeInitialize();
            ExpressionAst expression;
            if (!_values.TryGetValue(fieldName, out expression))
            {
                return default(TResult);
            }

            return UnwrapExpression<TResult>(expression);
        }
    }
}
