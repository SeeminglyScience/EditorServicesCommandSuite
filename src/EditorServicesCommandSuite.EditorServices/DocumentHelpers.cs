using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.EditorServices
{
    internal static class DocumentHelpers
    {
        private const string FileUriPrefix = "file:///";

        internal static string GetPathAsClientPath(string path)
        {
            if (path.StartsWith("untitled:", StringComparison.Ordinal))
            {
                return path;
            }

            Debug.Assert(
                !string.IsNullOrWhiteSpace(path),
                "Caller should verify path is valid");

            if (path.StartsWith(FileUriPrefix, StringComparison.Ordinal))
            {
                return path;
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return new Uri(path).AbsoluteUri;
            }

            // VSCode file URIs on Windows need the drive letter lowercase, and the colon
            // URI encoded. System.Uri won't do that, so we manually create the URI.
            var newUri = new StringBuilder(HttpUtility.UrlPathEncode(path));
            int colonIndex = path.IndexOf(Symbols.Colon);
            for (var i = colonIndex - 1; i >= 0; i--)
            {
                newUri.Remove(i, 1);
                newUri.Insert(i, char.ToLowerInvariant(path[i]));
            }

            return newUri
                .Remove(colonIndex, 1)
                .Insert(colonIndex, "%3A")
                .Replace(Symbols.Backslash, Symbols.ForwardSlash)
                .Insert(0, FileUriPrefix)
                .ToString();
        }
    }
}
