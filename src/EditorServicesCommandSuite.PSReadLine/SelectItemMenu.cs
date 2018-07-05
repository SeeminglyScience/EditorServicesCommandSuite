using System;
using System.Globalization;
using System.Management.Automation;
using System.Text;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal class SelectItemMenu<TItem>
    {
        internal int _startLeft;

        internal int _startTop;

        internal TItem[] _items;

        internal Func<TItem, string> _itemRenderer;

        internal StringBuilder _menuBuffer = new StringBuilder();

        internal StringBuilder _inputBuffer = new StringBuilder();

        internal SelectItemMenu(TItem[] items) : this(items, DefaultItemRenderer)
        {
        }

        internal SelectItemMenu(TItem[] items, Func<TItem, string> itemRenderer)
        {
            _startLeft = Console.CursorLeft;
            _startTop = Console.CursorTop;
            _items = items;
            _itemRenderer = itemRenderer;
        }

        internal string Caption { get; set; }

        internal string Message { get; set; }

        private static string DefaultItemRenderer(TItem item)
        {
            return LanguagePrimitives.ConvertTo<string>(item);
        }

        private TItem InputLoop()
        {
            var inputAccepted = false;
            while (!inputAccepted)
            {
                Render();
                ConsoleKeyInfo pressedKey = Console.ReadKey(true);
                switch (pressedKey.Key)
                {
                    case ConsoleKey.Backspace:
                        if (_inputBuffer.Length > 0)
                        {
                            _inputBuffer.Remove(_inputBuffer.Length - 1, 1);
                        }
                        break;
                    case ConsoleKey.Enter:
                        inputAccepted = true;
                        break;
                    default:
                        _inputBuffer.Append(pressedKey.KeyChar);
                        break;
                }
            }
            return default(TItem);
        }

        private void Render()
        {
            BufferItems();
            _menuBuffer
                .Append(Caption)
                .Append(':', ' ');

            if (_inputBuffer.Length > 0)
            {
                _menuBuffer.Append(_inputBuffer);

            }
            else
            {
                _menuBuffer.Append(Message);
            }
        }

        private void BufferItems()
        {
            for (var i = 0; i < _items.Length; i++)
            {
                BufferItem(_items[i], i);
            }
        }

        private void BufferItem(TItem item, int index)
        {
            _menuBuffer.AppendFormat(
                CultureInfo.CurrentCulture,
                "{0} - {1}", index + 1, _itemRenderer(item));
            _menuBuffer.AppendLine();
        }
    }
}
