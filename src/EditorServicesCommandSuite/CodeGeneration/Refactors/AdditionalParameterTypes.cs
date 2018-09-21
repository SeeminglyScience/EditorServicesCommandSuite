namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    /// <summary>
    /// Represents what additional parameter types should be included.
    /// </summary>
    public enum AdditionalParameterTypes
    {
        /// <summary>
        /// Indicates that only bound parameters should be added to the splat expression.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that unbound mandatory parameters should be included in the splat expression.
        /// </summary>
        Mandatory = 1,

        /// <summary>
        /// Indicates that all resolved parameters should be included in the splat expression.
        /// </summary>
        All = 2,
    }
}
