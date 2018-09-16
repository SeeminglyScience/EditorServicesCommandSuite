using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace EditorServicesCommandSuite.PSReadLine
{
    internal static class Menus
    {
        private static int s_alternateBufferCount;

        internal static bool IsInAlternateBuffer
        {
            get
            {
                return Interlocked.CompareExchange(ref s_alternateBufferCount, 0, 0) > 0;
            }
        }

        internal static InputPromptMenu InputPrompt(string caption, string message)
        {
            return new InputPromptMenu(caption, message);
        }

        internal static SelectItemMenu<TItem> ItemSelect<TItem>(
            string caption,
            string message,
            IEnumerable<TItem> items)
        {
            return new SelectItemMenu<TItem>(
                caption,
                message,
                items.ToArray());
        }

        internal static IDisposable NewAlternateBuffer()
        {
            Interlocked.Increment(ref s_alternateBufferCount);
            return new MenuHandle();
        }

        private class MenuHandle : IDisposable
        {
            internal MenuHandle()
            {
                Console.Write(Ansi.EnterAlternateBuffer);
            }

            public void Dispose()
            {
                Console.Write(Ansi.ExitAlternateBuffer);
                Interlocked.Decrement(ref s_alternateBufferCount);
            }
        }
    }
}
