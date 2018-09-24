using System.ComponentModel;
using System.Diagnostics;
using System.Management.Automation;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    /// <summary>
    /// Provides externally consumable information regarding available refactor providers.
    /// </summary>
    public sealed class RefactorProviderInfo
    {
        private readonly IDocumentRefactorProvider _provider;

        internal RefactorProviderInfo(IDocumentRefactorProvider provider, FunctionInfo command)
        {
            Debug.Assert(provider != null, nameof(provider));
            Debug.Assert(provider != null, nameof(command));
            _provider = provider;
            Command = command;
        }

        /// <summary>
        /// Gets the name of the refactor option.
        /// </summary>
        public string Name => _provider.Name ?? string.Empty;

        /// <summary>
        /// Gets the description of the refactor option.
        /// </summary>
        public string Description => _provider.Description ?? string.Empty;

        /// <summary>
        /// Gets the type of language elements that the refactor can target.
        /// </summary>
        public RefactorKind Targets => _provider.Kind;

        /// <summary>
        /// Gets the command that invokes this refactor option.
        /// </summary>
        [Hidden, EditorBrowsable(EditorBrowsableState.Never)]
        public FunctionInfo Command { get; }
    }
}
