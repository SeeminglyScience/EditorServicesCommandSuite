namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    /// <summary>
    /// Represents the type of string expression to convert to.
    /// </summary>
    public enum StringEnclosureType
    {
        /// <summary>
        /// Indicates that the caller should prompt for an enclosure selection.
        /// </summary>
        Prompt,

        /// <summary>
        /// Indicates that the string should not be enclosed.
        /// </summary>
        BareWord,

        /// <summary>
        /// Indicates that the string should be enclosed in single quotes.
        /// </summary>
        Literal,

        /// <summary>
        /// Indicates that the string should be enclosed in double quotes.
        /// </summary>
        Expandable,

        /// <summary>
        /// Indicates that the string should be enclosed in a single quote here-string expression.
        /// </summary>
        LiteralHereString,

        /// <summary>
        /// Indicates that the string should be enclosed in a double quote here-string expression.
        /// </summary>
        ExpandableHereString,
    }
}
