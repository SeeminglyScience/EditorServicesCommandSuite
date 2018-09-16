using System;
using System.Collections.Generic;

namespace EditorServicesCommandSuite.CodeGeneration
{
    internal static class DocumentEditWriterExtensions
    {
        internal static void WriteEachWithSeparator<TInput>(
            this DocumentEditWriter editWriter,
            IList<TInput> source,
            Action<TInput> writer,
            char separator)
        {
            editWriter.WriteEachWithSeparator<TInput>(
                source,
                writer,
                () => editWriter.Write(separator));
        }

        internal static void WriteEachWithSeparator<TInput>(
            this DocumentEditWriter editWriter,
            IList<TInput> source,
            Action<TInput> writer,
            char[] separator)
        {
            editWriter.WriteEachWithSeparator<TInput>(
                source,
                writer,
                () => editWriter.Write(separator));
        }

        internal static void WriteEachWithSeparator<TInput>(
            this DocumentEditWriter editWriter,
            IList<TInput> source,
            Action<TInput> writer,
            string separator)
        {
            editWriter.WriteEachWithSeparator<TInput>(
                source,
                writer,
                () => editWriter.Write(separator));
        }

        internal static void WriteEachWithSeparator<TInput>(
            this DocumentEditWriter editWriter,
            IEnumerable<TInput> source,
            Action<TInput> writer,
            char separator)
        {
            editWriter.WriteEachWithSeparator<TInput>(
                source,
                writer,
                () => editWriter.Write(separator));
        }

        internal static void WriteEachWithSeparator<TInput>(
            this DocumentEditWriter editWriter,
            IEnumerable<TInput> source,
            Action<TInput> writer,
            char[] separator)
        {
            editWriter.WriteEachWithSeparator<TInput>(
                source,
                writer,
                () => editWriter.Write(separator));
        }

        internal static void WriteEachWithSeparator<TInput>(
            this DocumentEditWriter editWriter,
            IEnumerable<TInput> source,
            Action<TInput> writer,
            string separator)
        {
            editWriter.WriteEachWithSeparator<TInput>(
                source,
                writer,
                () => editWriter.Write(separator));
        }
    }
}
