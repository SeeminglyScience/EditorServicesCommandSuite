using System;
using System.Collections.Generic;
using System.Management.Automation;
using EditorServicesCommandSuite.CodeGeneration.Refactors;

namespace EditorServicesCommandSuite.Commands
{
    /// <summary>
    /// Provides the implementation for the "Get-RefactorOption" cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "RefactorOption")]
    public class GetRefactorOptionCommand : PSCmdlet
    {
        private readonly HashSet<string> _alreadyReturned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private RefactorProviderInfo[] _infos;

        private RefactorKind? _targetType;

        /// <summary>
        /// Gets or sets the name(s) of providers to return.
        /// </summary>
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        [SupportsWildcards]
        [ValidateNotNullOrEmpty]
        [ArgumentCompleter(typeof(RefactorNameCompleter))]
        public string[] Name { get; set; }

        /// <summary>
        /// Gets or sets the target refactor kind.
        /// </summary>
        [Parameter(Position = 1)]
        public RefactorKind TargetType
        {
            get
            {
                return _targetType.GetValueOrDefault();
            }

            set
            {
                _targetType = value;
            }
        }

        /// <summary>
        /// The BeginProcessing method. See <see ref="PSCmdlet.BeginProcessing" />.
        /// </summary>
        protected override void BeginProcessing()
        {
            _infos = RefactorNameCompleter.GetRefactorOptions(this);
        }

        /// <summary>
        /// The ProcessRecord method. See <see ref="PSCmdlet.ProcessRecord" />.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (Name == null)
            {
                foreach (RefactorProviderInfo info in _infos)
                {
                    ProcessSingleProvider(info);
                }

                return;
            }

            foreach (string name in Name)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                WildcardPattern pattern = WildcardPattern.ContainsWildcardCharacters(name)
                    ? new WildcardPattern(name, WildcardOptions.IgnoreCase)
                    : null;

                foreach (RefactorProviderInfo info in _infos)
                {
                    ProcessSingleProvider(info, pattern, name);
                }
            }
        }

        private void ProcessSingleProvider(
            RefactorProviderInfo info,
            WildcardPattern pattern = null,
            string name = null)
        {
            if (_targetType != null && !info.Targets.HasFlag(_targetType.Value))
            {
                return;
            }

            if (pattern == null)
            {
                if (name != null && !info.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase))
                {
                    return;
                }
            }
            else if (!pattern.IsMatch(info.Name))
            {
                return;
            }

            if (!_alreadyReturned.Add(info.Name))
            {
                return;
            }

            WriteObject(info);
        }
    }
}
