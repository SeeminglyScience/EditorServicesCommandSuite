using System;
using System.Text;

namespace EditorServicesCommandSuite.Utility
{
    internal static class StringExtensions
    {
        internal static StringBuilder Append(this StringBuilder sb, params char[] value)
        {
            return sb.Append(value);
        }

        internal static bool Contains(this string source, char value)
        {
            return source != null && source.IndexOf(value) != -1;
        }

        internal static bool Contains(this string source, char[] value)
        {
            if (string.IsNullOrEmpty(source) || value == null)
            {
                return false;
            }

            if (value.Length == 0)
            {
                return true;
            }

            for (int index = 0; index != -1; index++)
            {
                index = source.IndexOf(value[0], index);
                if (index == -1)
                {
                    return false;
                }

                if (index + value.Length > source.Length)
                {
                    return false;
                }

                bool isMatch = true;
                for (var i = 1; i < value.Length; i++)
                {
                    if (source[index + i] != value[i])
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool EndsWith(
            this string source,
            char[] value,
            StringComparison comparisonType)
        {
            if (comparisonType == StringComparison.Ordinal)
            {
                return EndsWith(source, value, ignoreCase: false);
            }

            if (comparisonType == StringComparison.OrdinalIgnoreCase)
            {
                return EndsWith(source, value, ignoreCase: true);
            }

            throw new NotSupportedException(comparisonType.ToString());
        }

        internal static bool EndsWith(
            this string source,
            char value,
            StringComparison comparisonType)
        {
            if (comparisonType == StringComparison.Ordinal)
            {
                return EndsWith(source, value, ignoreCase: false);
            }

            if (comparisonType == StringComparison.OrdinalIgnoreCase)
            {
                return EndsWith(source, value, ignoreCase: true);
            }

            throw new NotSupportedException(comparisonType.ToString());
        }

        internal static bool EndsWith(this string source, char[] value, bool ignoreCase = false)
        {
            return StartsWith(
                ReverseChars(source.ToCharArray()),
                ReverseChars(value),
                ignoreCase);
        }

        internal static bool EndsWith(this string source, char value, bool ignoreCase = false)
        {
            return StartsWith(
                ReverseChars(source.ToCharArray()),
                value,
                ignoreCase);
        }

        internal static bool StartsWith(this string source, char[] value)
        {
            return StartsWith(source, value, ignoreCase: false);
        }

        internal static bool StartsWith(this string source, char value)
        {
            return StartsWith(source, value, ignoreCase: false);
        }

        internal static bool StartsWith(
            this string source,
            char[] value,
            StringComparison comparisonType)
        {
            if (comparisonType == StringComparison.Ordinal)
            {
                return StartsWith(source, value, ignoreCase: false);
            }

            if (comparisonType == StringComparison.OrdinalIgnoreCase)
            {
                return StartsWith(source, value, ignoreCase: true);
            }

            throw new NotSupportedException(comparisonType.ToString());
        }

        internal static bool StartsWith(
            this string source,
            char value,
            StringComparison comparisonType)
        {
            if (comparisonType == StringComparison.Ordinal)
            {
                return StartsWith(source, value, ignoreCase: false);
            }

            if (comparisonType == StringComparison.OrdinalIgnoreCase)
            {
                return StartsWith(source, value, ignoreCase: true);
            }

            throw new NotSupportedException(comparisonType.ToString());
        }

        internal static bool StartsWith(
            this string source,
            char[] value,
            bool ignoreCase)
        {
            return StartsWith(
                source.ToCharArray(),
                value,
                ignoreCase);
        }

        internal static bool StartsWith(
            this string source,
            char value,
            bool ignoreCase)
        {
            return StartsWith(
                source.ToCharArray(),
                value,
                ignoreCase);
        }

        private static bool StartsWith(
            char[] source,
            char[] value,
            bool ignoreCase)
        {
            if (source == null || value == null)
            {
                return false;
            }

            if (source.Length < value.Length)
            {
                return false;
            }

            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] == source[i])
                {
                    continue;
                }

                if (!ignoreCase)
                {
                    return false;
                }

                if (char.ToUpperInvariant(value[i]) != char.ToUpperInvariant(source[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool StartsWith(
            char[] source,
            char value,
            bool ignoreCase)
        {
            if (source == null || source.Length == 0)
            {
                return false;
            }

            if (value == source[0])
            {
                return true;
            }

            if (!ignoreCase)
            {
                return false;
            }

            return char.ToUpperInvariant(value) == char.ToUpperInvariant(source[0]);
        }

        private static char[] ReverseChars(char[] source)
        {
            var result = new char[source.Length];
            for (var i = 0; i < source.Length / 2; i++)
            {
                result[i] = source[source.Length - i - 1];
                result[source.Length - i - 1] = source[i];
            }

            if (result.Length % 2 == 0)
            {
                return result;
            }

            result[result.Length / 2] = source[result.Length / 2];
            return result;
        }
    }
}
