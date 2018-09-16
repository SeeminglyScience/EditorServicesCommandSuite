using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Reflection;

namespace EditorServicesCommandSuite.CodeGeneration.Refactors
{
    internal static class Cmdletizer
    {
        private static readonly XNamespace ns = "http://schemas.microsoft.com/cmdlets-over-objects/2009/11";

#pragma warning disable SA1116
        internal static void WriteRefactorModule(string path)
        {
            new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement(ns + "PowerShellMetadata",
                    new XElement(ns + "Class",
                        new XAttribute("ClassName", typeof(IDocumentRefactorProvider).Name),
                        new XAttribute("CmdletAdapter", typeof(RefactorCmdletAdapter).ToString()),
                        new XElement(ns + "Version", "1.0"),
                        new XElement(ns + "DefaultNoun", "Refactor"),
                        new XElement(ns + "StaticCmdlets",
                        GetRefactorProviders()
                            .Select(GetElementForProvider)))))
                .Save(path);
        }

        internal static XElement GetElementForProvider(Type provider)
        {
            RefactorAttribute metadata = provider.GetAttribute<RefactorAttribute>(false);

            return new XElement(ns + "Cmdlet",
                new XElement(ns + "CmdletMetadata",
                    new XAttribute("Verb", metadata.Verb),
                    new XAttribute("Noun", metadata.Noun)),
                new XElement(ns + "Method",
                    new XAttribute("MethodName", provider.Name),
                    new XElement(ns + "ReturnValue",
                        new XElement(ns + "Type",
                            new XAttribute("PSType", "System.Void"))),
                    GetParameterElement(provider)));
        }

        private static XElement GetParameterElement(Type provider)
        {
            RefactorConfigurationAttribute metadata = provider
                .GetCustomAttributes(
                    typeof(RefactorConfigurationAttribute),
                    inherit: true)
                .OfType<RefactorConfigurationAttribute>()
                .FirstOrDefault();

            var outputParameter = new XElement(ns + "Parameter",
                new XAttribute("ParameterName", "CmdletOutput"),
                new XElement(ns + "Type",
                    new XAttribute("PSType", "System.Void")),
                new XElement(ns + "CmdletOutputMetadata"));

            if (metadata == null)
            {
                return new XElement(ns + "Parameters", outputParameter);
            }

            return new XElement(ns + "Parameters",
                metadata
                    .ConfigurationType
                    .GetProperties()
                    .Where(p => p.IsDefined(typeof(ParameterAttribute), inherit: true))
                    .Select(GetParameterFromProperty)
                    .Concat(new XElement[] { outputParameter }));
        }

        private static XElement GetParameterFromProperty(PropertyInfo property)
        {
            ParameterAttribute parameterAttr = property.GetCustomAttribute<ParameterAttribute>();
            var metadataNode = new XElement(ns + "CmdletParameterMetadata");
            if (parameterAttr.Mandatory)
            {
                metadataNode.Add(new XAttribute("IsMandatory", "true"));
            }

            if (parameterAttr.Position != int.MinValue)
            {
                metadataNode.Add(new XAttribute("Position", parameterAttr.Position));
            }

            if (property.IsDefined<ValidateNotNullAttribute>(true))
            {
                metadataNode.Add(new XElement(ns + "ValidateNotNull"));
            }

            if (property.IsDefined<ValidateNotNullOrEmptyAttribute>(true))
            {
                metadataNode.Add(new XElement(ns + "ValidateNotNullOrEmpty"));
            }

            return new XElement(ns + "Parameter",
                new XAttribute("ParameterName", property.Name),
                new XElement(ns + "Type",
                    new XAttribute("PSType", property.PropertyType.ToString())),
                metadataNode);
        }

        private static string EscapedTypeName(Type type)
        {
            string defaultName = type.ToString();
            if (defaultName.IndexOf('`') == -1)
            {
                return defaultName;
            }

            StringBuilder newName = new StringBuilder(defaultName);
            int lastBracket = -1;
            for (var i = defaultName.Length - 1; i <= 0; i--)
            {
                if (i == '[')
                {
                    lastBracket = i;
                    continue;
                }

                if (i == '`')
                {
                    if (lastBracket == -1)
                    {
                        continue;
                    }

                    newName.Remove(i, i - lastBracket);
                    lastBracket = -1;
                }
            }

            return newName.ToString();
        }

        private static IEnumerable<Type> GetRefactorProviders()
        {
            return typeof(Cmdletizer)
                .Assembly
                .GetModules()
                .SelectMany(m => m.FindTypes(FilterIsRefactorProvider, null));
        }

        private static bool FilterIsRefactorProvider(Type type, object filterCriteria)
        {
            return typeof(IDocumentRefactorProvider).IsAssignableFrom(type)
                && type.IsDefined(typeof(RefactorAttribute), inherit: false);
        }
#pragma warning restore SA1116
    }
}
