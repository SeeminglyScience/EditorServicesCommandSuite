using System;

using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Represents an edit to a document.
    /// </summary>
    internal class DocumentEdit
    {
        private static int s_lastId = 1;

        /// <summary>
        /// Gets or sets the starting offset of the edit.
        /// </summary>
        public long StartOffset { get; set; }

        /// <summary>
        /// Gets or sets the ending offset of the edit.
        /// </summary>
        public long EndOffset { get; set; }

        /// <summary>
        /// Gets or sets the original text that the edit will be replacing.
        /// </summary>
        public string OriginalValue { get; set; }

        /// <summary>
        /// Gets or sets the new text that will be replacing the existing text.
        /// </summary>
        public string NewValue { get; set; }

        /// <summary>
        /// Gets or sets the name of the file the edit takes place in.
        /// </summary>
        public string FileName { get; set; }

        public Uri Uri => string.IsNullOrEmpty(FileName)
            ? null
            : new Uri(DocumentHelpers.GetPathAsClientPath(FileName));

        internal int Id { get; } = s_lastId++;
    }
}
