using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.CodeGeneration
{
    internal sealed class WorkspaceChange
    {
        private WorkspaceChange(
            WorkspaceChangeType type,
            string location,
            string value,
            DocumentEdit[] edits,
            IScriptExtent selection,
            Command command)
        {
            Type = type;
            Location = location;
            Value = value;
            Edits = edits ?? Array.Empty<DocumentEdit>();
            Selection = selection;
            Command = command;
        }

        public WorkspaceChangeType Type { get; }

        public string Location { get; }

        public string Value { get; }

        public DocumentEdit[] Edits { get; }

        public IScriptExtent Selection { get; }

        public Command Command { get; }

        public static WorkspaceChange NewDocument(string location, string content = "")
        {
            return new WorkspaceChange(
                WorkspaceChangeType.New,
                location ?? throw new ArgumentNullException(nameof(location)),
                content ?? throw new ArgumentNullException(nameof(content)),
                edits: null,
                selection: null,
                command: null);
        }

        public static WorkspaceChange DeleteDocument(string location)
        {
            return new WorkspaceChange(
                WorkspaceChangeType.New,
                location ?? throw new ArgumentNullException(nameof(location)),
                value: null,
                edits: null,
                selection: null,
                command: null);
        }

        public static WorkspaceChange RenameDocument(string location, string newName)
        {
            return new WorkspaceChange(
                WorkspaceChangeType.Rename,
                location ?? throw new ArgumentNullException(nameof(location)),
                newName ?? throw new ArgumentNullException(nameof(newName)),
                edits: null,
                selection: null,
                command: null);
        }

        public static WorkspaceChange MoveDocument(string location, string destination)
        {
            return new WorkspaceChange(
                WorkspaceChangeType.Move,
                location ?? throw new ArgumentNullException(nameof(location)),
                destination ?? throw new ArgumentNullException(nameof(destination)),
                edits: null,
                selection: null,
                command: null);
        }

        public static WorkspaceChange EditDocument(string location, IEnumerable<DocumentEdit> edits)
        {
            return new WorkspaceChange(
                WorkspaceChangeType.Edit,
                location /* ?? throw new ArgumentNullException(nameof(location)) */,
                value: null,
                edits?.ToArray(),
                selection: null,
                command: null);
        }

        public static WorkspaceChange[] EditDocuments(IEnumerable<DocumentEdit> edits)
        {
            return edits.ToLookup(edit => edit.FileName, PathUtils.PathComparer)
                .Select(group => EditDocument(group.Key, group.ToArray()))
                .ToArray();
        }

        public static WorkspaceChange InvokeCommand(string name, object arguments = null)
        {
            return new WorkspaceChange(
                WorkspaceChangeType.Command,
                location: null,
                value: null,
                edits: null,
                selection: null,
                command: new Command(name, arguments));
        }

        public static WorkspaceChange SetContext(IScriptExtent selection)
        {
            return new WorkspaceChange(
                WorkspaceChangeType.Context,
                location: null,
                value: null,
                edits: null,
                selection,
                command: null);
        }
    }
}
