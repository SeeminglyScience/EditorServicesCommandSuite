namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    /// <summary>
    /// Represents the placement of a new generated function.
    /// </summary>
    public enum ExtractFunctionDestination
    {
        /// <summary>
        /// Indicates that the caller should prompt for a destination.
        /// </summary>
        Prompt,

        /// <summary>
        /// Indicates that the function should be placed above the extracted text.
        /// </summary>
        Inline,

        /// <summary>
        /// Indicates that the function should be placed in the closest Begin block.
        /// </summary>
        Begin,

        /// <summary>
        /// Indicates that the function should be placed in a new file.
        /// </summary>
        NewFile,
    }
}
