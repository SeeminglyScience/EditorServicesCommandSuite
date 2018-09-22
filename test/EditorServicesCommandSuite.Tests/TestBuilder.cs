using System;
using System.Globalization;
using System.Text;

namespace EditorServicesCommandSuite.Tests
{
    /// <summary>
    /// Provides an easy way to write consistent test strings.
    /// </summary>
    public class TestBuilder
    {
        private const string DefaultNewLine = "\n";

        private const string DefaultTab = "\t";

        private const string CursorPosition = "{{c}}";

        private const string SelectionStart = "{{ss}}";

        private const string SelectionEnd = "{{se}}";

        private readonly string _newLine;

        private readonly string _tab;

        private readonly StringBuilder _builder;

        private bool _selectionState;

        internal TestBuilder(string newLineString, string tabString)
        {
            _newLine = newLineString;
            _tab = tabString;
            _builder = new StringBuilder();
        }

        public static implicit operator string(TestBuilder builder)
        {
            return builder._builder.ToString();
        }

        internal static TestBuilder Create(string newLineString = null, string tabString = null)
        {
            return new TestBuilder(
                newLineString ?? DefaultNewLine,
                tabString ?? DefaultTab);
        }

        /// <summary>
        /// Adds the cursor position string to the test string.
        /// </summary>
        /// <returns>An instance of this object after the operation is completed.</returns>
        public TestBuilder Cursor()
        {
            _builder.Append(CursorPosition);
            return this;
        }

        /// <summary>
        /// Adds either the selection start or selection end string to the test string.
        /// If this method has been called on this object previously, it will add the
        /// selection end string.  Otherwise it will add the selection start string.
        /// </summary>
        /// <returns>An instance of this object after the operation is completed.</returns>
        public TestBuilder Selection()
        {
            if (_selectionState)
            {
                _selectionState = false;
                _builder.Append(SelectionEnd);
                return this;
            }

            _selectionState = true;
            _builder.Append(SelectionStart);
            return this;
        }

        /// <summary>
        /// Adds the selection start and selection end strings to the test string before and
        /// after (respectively) the specified text is added.
        /// </summary>
        /// <param name="text">The text that should be indicated as selected.</param>
        /// <returns>An instance of this object after the operation is completed.</returns>
        public TestBuilder Selected(string text)
        {
            _builder
                .Append(SelectionStart)
                .Append(text)
                .Append(SelectionEnd);

            return this;
        }

        /// <summary>
        /// Adds the selection start and selection end strings to the test string before and
        /// after (respectively) the specified delegate is invoked.
        /// </summary>
        /// <param name="action">The delegate to invoke between selection start and end.</param>
        /// <returns>An instance of this object after the operation is completed.</returns>
        public TestBuilder Selected(Action<TestBuilder> action)
        {
            _builder.Append(SelectionStart);
            action(this);
            _builder.Append(SelectionEnd);
            return this;
        }

        /// <summary>
        /// Adds text without a new line. Even though the name is plural, it only accepts a
        /// single string. This is just so when using it the cursor is at a tab stop after
        /// the opening double quote. Really needs a better name.
        /// </summary>
        /// <param name="text">The text to add to the test string.</param>
        /// <param name="hasCursor">
        /// A value indicating that <paramref name="text" /> is a format string that accepts a
        /// single format argument of the cursor position string.
        /// </param>
        /// <param name="hasSelection">
        /// A value indicating that <paramref name="text" /> is a format string that accepts two
        /// format arguments. The first argument will be the selection start string, and the second
        /// will be the selection end string.
        /// </param>
        /// <returns>An instance of this object after the operation is completed.</returns>
        public TestBuilder Texts(string text, bool hasCursor = false, bool hasSelection = false)
        {
            if (text == null)
            {
                return this;
            }

            text = text.Replace("    ", _tab);
            if (hasCursor)
            {
                _builder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    text,
                    CursorPosition);
                return this;
            }

            if (hasSelection)
            {
                _builder.AppendFormat(
                    CultureInfo.InvariantCulture,
                    text,
                    SelectionStart,
                    SelectionEnd);
                return this;
            }

            _builder.Append(text);
            return this;
        }

        /// <summary>
        /// Adds text with a new line. Even though the name is plural, it only accepts a
        /// single string. This is just so when using it the cursor is at a tab stop after
        /// the opening double quote. Really needs a better name.
        /// </summary>
        /// <param name="text">The text to add to the test string.</param>
        /// <param name="hasCursor">
        /// A value indicating that <paramref name="text" /> is a format string that accepts a
        /// single format argument of the cursor position string.
        /// </param>
        /// <param name="hasSelection">
        /// A value indicating that <paramref name="text" /> is a format string that accepts two
        /// format arguments. The first argument will be the selection start string, and the second
        /// will be the selection end string.
        /// </param>
        /// <returns>An instance of this object after the operation is completed.</returns>
        public TestBuilder Lines(string text = null, bool hasCursor = false, bool hasSelection = false)
        {
            if (text == null)
            {
                _builder.Append(_newLine);
                return this;
            }

            Texts(text, hasCursor, hasSelection);
            _builder.Append(_newLine);
            return this;
        }
    }
}
