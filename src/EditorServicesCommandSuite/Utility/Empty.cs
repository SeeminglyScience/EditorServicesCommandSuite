using System.Management.Automation.Language;
using System.Text.RegularExpressions;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.Utility
{
    internal static class Empty
    {
        public static readonly MatchCollection MatchCollection = Regex.Matches(string.Empty, "z");

        public static IScriptExtent Extent => new EmptyExtent();

        public static IScriptPosition Position => new EmptyPosition();

        public static T[] Array<T>()
        {
            return EmptyArray<T>.Instance;
        }

        private static class EmptyArray<T>
        {
            internal static readonly T[] Instance = new T[0];
        }
    }
}
