namespace EditorServicesCommandSuite.Internal
{
    /// <summary>
    /// Represents an edit to a document.
    /// </summary>
    public class DocumentEdit
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

        internal int Id { get; } = s_lastId++;
    }
}
