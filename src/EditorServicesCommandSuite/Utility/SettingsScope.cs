namespace EditorServicesCommandSuite.Utility
{
    /// <summary>
    /// Represents a scope in which CommandSuite settings are stored.
    /// </summary>
    public enum SettingsScope
    {
        /// <summary>
        /// Scope for settings local to the current process.
        /// </summary>
        Process,

        /// <summary>
        /// Scope for settings local to the current workspace.
        /// </summary>
        Workspace,

        /// <summary>
        /// Scope for settings local to the current user.
        /// </summary>
        User,

        /// <summary>
        /// Scope for settings that apply to all users on the current machine.
        /// </summary>
        Machine,
    }
}
