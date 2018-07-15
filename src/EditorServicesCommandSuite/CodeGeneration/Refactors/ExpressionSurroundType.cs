namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    /// <summary>
    /// Represents the type of expression to wrap the selection in.
    /// </summary>
    public enum ExpressionSurroundType
    {
        /// <summary>
        /// Indicates that the caller should prompt for a surround type.
        /// </summary>
        Prompt,

        /// <summary>
        /// Indicates that the selection should be wrapped in an if statement.
        /// </summary>
        IfStatement,

        /// <summary>
        /// Indicates that the selection should be wrapped in an while statement.
        /// </summary>
        WhileStatement,

        /// <summary>
        /// Indicates that the selection should be wrapped in an foreach statement.
        /// </summary>
        ForeachStatement,

        /// <summary>
        /// Indicates that the selection should be wrapped in parenthesis.
        /// </summary>
        ParenExpression,

        /// <summary>
        /// Indicates that the selection should be wrapped in an array initalizer. (i.e. "@()")
        /// </summary>
        ArrayInitializer,

        /// <summary>
        /// Indicates that the selection should be wrapped in a dollar paren expression. (i.e. "$()")
        /// </summary>
        DollarParenExpression,
    }
}
