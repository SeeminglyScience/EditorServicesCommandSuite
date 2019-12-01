using System;
using System.Globalization;

namespace EditorServicesCommandSuite.Utility
{
    internal static class Validate
    {
        public static void Is<T>(string parameterName, T valueToCheck, Func<T, bool> predicate, string messageToThrow)
        {
            if (predicate(valueToCheck))
            {
                return;
            }

            throw new ArgumentException(messageToThrow, parameterName);
        }

        public static void IsNotNull(string parameterName, object valueToCheck)
        {
            if (valueToCheck != null)
            {
                return;
            }

            throw new ArgumentNullException(parameterName);
        }

        public static void IsNotNullOrEmpty(string parameterName, string valueToCheck)
        {
            if (!string.IsNullOrEmpty(valueToCheck))
            {
                return;
            }

            throw new ArgumentNullException(parameterName);
        }

        public static void IsNotNullOrWhiteSpace(string parameterName, string valueToCheck)
        {
            if (!string.IsNullOrWhiteSpace(valueToCheck))
            {
                return;
            }

            throw new ArgumentNullException(parameterName);
        }

        public static void IsWithinRange(
            string parameterName,
            int valueToCheck,
            int minimum,
            int maximum)
        {
            if (!(valueToCheck < minimum || valueToCheck > maximum))
            {
                return;
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "Value is not between {0} and {1}.",
                    minimum,
                    maximum),
                parameterName);
        }
    }
}
