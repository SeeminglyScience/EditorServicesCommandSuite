using System;
using System.Globalization;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Language;
using EditorServicesCommandSuite.Language;

namespace EditorServicesCommandSuite.Utility
{
    internal static class Error
    {
        public static PSArgumentOutOfRangeException OutOfRange(string paramName)
        {
            return new PSArgumentOutOfRangeException(paramName);
        }

        public static PSArgumentException TypeNotFound(string typeName)
        {
            return new PSArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ImplementAbstractMethodsStrings.TypeNotFound,
                    typeName));
        }

        public static PSArgumentException InvalidTypeForPowerShellBase(string typeName)
        {
            return new PSArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ImplementAbstractMethodsStrings.InvalidTypeForPowerShellBase,
                    typeName));
        }

        public static PSArgumentException FunctionAlreadyExported(string functionName)
        {
            return new PSArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    RegisterCommandExportStrings.FunctionAlreadyExported,
                    functionName));
        }

        public static PSArgumentException CannotFindAst(string astType)
        {
            return new PSArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    RefactorStrings.CannotFindAst,
                    astType));
        }

        public static NodeNotFoundException AttemptedAccessDefaultTokenNode()
        {
            return new NodeNotFoundException(
                LanguageStrings.DefaultTokenNodeValueAccess,
                innerException: null,
                nameof(LanguageStrings.DefaultTokenNodeValueAccess),
                target: null);
        }

        public static NodeNotFoundException TokenNotFound(object criteria = null)
        {
            return new NodeNotFoundException(
                LanguageStrings.TokenNotFound,
                innerException: null,
                nameof(LanguageStrings.TokenNotFound),
                criteria);
        }

        public static NodeNotFoundException TokenKindNotFound(TokenKind kind)
        {
            return new NodeNotFoundException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    LanguageStrings.TokenKindNotFound,
                    kind),
                innerException: null,
                nameof(LanguageStrings.TokenKindNotFound),
                kind);
        }

        public static NodeNotFoundException TokenPositionNotFound(object position)
        {
            return new NodeNotFoundException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    LanguageStrings.TokenPositionNotFound,
                    position),
                innerException: null,
                nameof(LanguageStrings.TokenPositionNotFound),
                position);
        }

        public static PSInvalidOperationException ManifestRequired()
        {
            return new PSInvalidOperationException(RefactorStrings.ManifestRequired);
        }

        public static CommandNotFoundException CommandNotFound(string commandName)
        {
            return new CommandNotFoundException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    RefactorStrings.CommandNotFound,
                    commandName));
        }

        public static PSArgumentException CannotInferMember(CommandElementAst member)
        {
            return new PSArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    ExpandMemberExpressionStrings.CannotInferMember,
                    member));
        }

        public static PSInvalidOperationException InvalidOperation(string message)
        {
            return new PSInvalidOperationException(message);
        }

        public static PSArgumentException CannotExtractFromUnnamed()
        {
            return new PSArgumentException(ExtractFunctionStrings.CannotExtractFromUnnamed);
        }

        public static PSArgumentException CommandNotFound()
        {
            return new PSArgumentException(AddModuleQualificationStrings.CommandNameRequired);
        }

        public static PSInvalidOperationException CmdletRequired()
        {
            return new PSInvalidOperationException(AddModuleQualificationStrings.PSCmdletRequired);
        }

        public static ErrorRecord UntitledWorkspaceNotSupported()
        {
            return new ErrorRecord(
                new PSNotSupportedException(RefactorStrings.UntitledWorkspaceNotSupported),
                nameof(UntitledWorkspaceNotSupported),
                ErrorCategory.InvalidArgument,
                null);
        }

        public static ErrorRecord UntitledWorkspaceNotSupported(string message)
        {
            return new ErrorRecord(
                new PSNotSupportedException(message),
                nameof(UntitledWorkspaceNotSupported),
                ErrorCategory.InvalidArgument,
                null);
        }

        public static ErrorRecord InvalidScopeNoPath(SettingsScope scope)
        {
            return new ErrorRecord(
                new PSArgumentException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        SettingsFileStrings.InvalidScopeNoPath,
                        scope)),
                nameof(SettingsFileStrings.InvalidScopeNoPath),
                ErrorCategory.InvalidArgument,
                scope);
        }

        public static ErrorRecord ReadWriteFileNotFileSystemProvider(object actualProvider)
        {
            return new ErrorRecord(
                ReadWriteFileNotFileSystemProviderEx(actualProvider),
                nameof(PathStrings.ReadWriteFileNotFileSystemProvider),
                ErrorCategory.InvalidArgument,
                actualProvider);
        }

        public static PSInvalidOperationException ReadWriteFileNotFileSystemProviderEx(object actualProvider)
        {
            return new PSInvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.ReadWriteFileNotFileSystemProvider,
                    actualProvider));
        }

        public static ErrorRecord FileAlreadyExistsNoForce(string path)
        {
            return new ErrorRecord(
                FileAlreadyExistsNoForceEx(path),
                nameof(PathStrings.FileAlreadyExistsNoForce),
                ErrorCategory.WriteError,
                path);
        }

        public static IOException FileAlreadyExistsNoForceEx(string path)
        {
            return new IOException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.FileAlreadyExistsNoForce,
                    path));
        }

        public static ErrorRecord ParentMissingNoForce(string path)
        {
            return new ErrorRecord(
                ParentMissingNoForceEx(path),
                nameof(PathStrings.ParentDirectoryMissingNoForce),
                ErrorCategory.WriteError,
                path);
        }

        public static IOException ParentMissingNoForceEx(string path)
        {
            return new IOException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    PathStrings.ParentDirectoryMissingNoForce,
                    path));
        }

        public static ErrorRecord Wrap(Exception e)
        {
            if (e is RuntimeException rte)
            {
                return Wrap(rte);
            }

            return new ErrorRecord(
                e,
                "CommandSuiteUnexpectedError",
                ErrorCategory.NotSpecified,
                null);
        }

        public static ErrorRecord Wrap(RuntimeException rte)
        {
            return new ErrorRecord(rte.ErrorRecord, rte);
        }
    }
}
