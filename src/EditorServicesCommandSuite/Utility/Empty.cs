using System;
using System.Collections.Concurrent;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace EditorServicesCommandSuite.Utility
{
    internal static class Empty
    {
        public static readonly MatchCollection MatchCollection = Regex.Matches(string.Empty, "z");

        internal class ScriptAst : ScriptBlockAst
        {
            internal static readonly ScriptAst Untitled = Create(string.Empty);

            private ScriptAst(
                IScriptExtent extent,
                ParamBlockAst paramBlock,
                StatementBlockAst statements)
                : base(extent, paramBlock, statements, isFilter: false)
            {
            }

            internal static ScriptAst Get(string filePath = null)
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return Untitled;
                }

                return PerFileInstanceCache<ScriptAst>.Map
                    .GetOrAdd(filePath, Create);
            }

            private static ScriptAst Create(string filePath)
            {
                Empty.Extent ee = Empty.Extent.Get(filePath);
                return new ScriptAst(
                    ee,
                    null,
                    new StatementBlockAst(
                        ee,
                        Array.Empty<StatementAst>(),
                        Array.Empty<TrapStatementAst>()));
            }
        }

        internal class Extent : IScriptExtent
        {
            public static readonly Extent Untitled = new Extent(string.Empty);

            private Extent(string filePath)
            {
                File = filePath;
                StartScriptPosition = Empty.Position.Get(filePath);
                EndScriptPosition = StartScriptPosition;
            }

            public int EndColumnNumber => 0;

            public int EndLineNumber => 0;

            public int EndOffset => 0;

            public IScriptPosition EndScriptPosition { get; }

            public string File { get; }

            public int StartColumnNumber => 0;

            public int StartLineNumber => 0;

            public int StartOffset => 0;

            public IScriptPosition StartScriptPosition { get; }

            public string Text => string.Empty;

            internal static Extent Get(string filePath = null)
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return Untitled;
                }

                return PerFileInstanceCache<Extent>.Map.GetOrAdd(
                    filePath,
                    CreateExtent);
            }

            private static Extent CreateExtent(string filePath)
            {
                return new Extent(filePath);
            }
        }

        internal class Position : IScriptPosition
        {
            public static readonly Position Untitled = new Position(string.Empty);

            private Position(string filePath)
            {
                File = filePath;
            }

            public int ColumnNumber => 0;

            public string File { get; }

            public string Line => string.Empty;

            public int LineNumber => 0;

            public int Offset => 0;

            public string GetFullScript() => string.Empty;

            internal static Position Get(string filePath)
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return Untitled;
                }

                return PerFileInstanceCache<Position>.Map
                    .GetOrAdd(filePath, Create);
            }

            private static Position Create(string filePath)
            {
                return new Position(filePath);
            }
        }

        private static class PerFileInstanceCache<T>
        {
            internal static ConcurrentDictionary<string, T> Map =
                new ConcurrentDictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
