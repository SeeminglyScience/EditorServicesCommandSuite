namespace EditorServicesCommandSuite.Language
{
    /// <summary>
    /// Represents a style of capitalization for identifiers.
    /// </summary>
    public enum CaseType
    {
        /// <summary>
        /// Case style will default based on the action taken.
        /// </summary>
        Default = 0,

        /// <summary>
        /// The first letter of each word in the identifier will be capitalized.
        /// </summary>
        PascalCase = 1,

        /// <summary>
        /// The first letter of each word in the identifier will be capitalized,
        /// except for the first word which will be all lowercase.
        /// </summary>
        CamelCase = 2,
    }
}
