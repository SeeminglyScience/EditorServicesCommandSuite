using System;
using System.Globalization;
using System.Text;

namespace EditorServicesCommandSuite.Tests
{
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

        public TestBuilder Cursor()
        {
            _builder.Append(CursorPosition);
            return this;
        }

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

        public TestBuilder Selected(string text)
        {
            _builder
                .Append(SelectionStart)
                .Append(text)
                .Append(SelectionEnd);

            return this;
        }

        public TestBuilder Selected(Action<TestBuilder> action)
        {
            _builder.Append(SelectionStart);
            action(this);
            _builder.Append(SelectionEnd);
            return this;
        }

        public TestBuilder Text(string text, bool hasCursor = false, bool hasSelection = false)
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

        public TestBuilder Line(string text = null, bool hasCursor = false, bool hasSelection = false)
        {
            if (text == null)
            {
                _builder.Append(_newLine);
                return this;
            }

            Text(text, hasCursor, hasSelection);
            _builder.Append(_newLine);
            return this;
        }
    }
}
