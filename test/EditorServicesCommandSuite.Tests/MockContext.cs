using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Text;
using System.Threading.Tasks;
using EditorServicesCommandSuite.CodeGeneration.Refactors;
using EditorServicesCommandSuite.Internal;
using EditorServicesCommandSuite.Language;
using EditorServicesCommandSuite.Utility;

namespace EditorServicesCommandSuite.Tests
{
    public class MockContext
    {
        public static async Task<string> GetRefactoredTextAsync(
            string testString,
            Func<DocumentContextBase, Task<IEnumerable<DocumentEdit>>> editFactory)
        {
            Settings.SetSetting("NewLineCharacter", "\n");
            Settings.SetSetting("TabString", "\t");
            var context = GetContext(testString);
            var sb = new StringBuilder(context.RootAst.Extent.Text);
            foreach (var edit in (await editFactory(context)).OrderByDescending(e => e.StartOffset))
            {
                if (edit == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(edit.OriginalValue))
                {
                    sb.Remove((int)edit.StartOffset, edit.OriginalValue.Length);
                }

                sb.Insert((int)edit.StartOffset, edit.NewValue);
                // sb.Replace(
                //     edit.OriginalValue,
                //     edit.NewValue,
                //     (int)edit.StartOffset,
                //     1);
            }

            return sb.ToString();
        }

        public static DocumentContextBase GetContext(string testString)
        {
            var sb = new StringBuilder(testString);
            var cursor = testString.IndexOf("{{c}}");
            if (cursor >= 0)
            {
                sb.Remove(cursor, 5);
            }
            else
            {
                cursor = testString.Length - 1;
            }

            var selectionStart = testString.IndexOf("{{ss}}");
            var selectionEnd = 0;
            if (selectionStart >= 0)
            {
                sb.Remove(selectionStart, 6);
                selectionEnd = testString.IndexOf("{{se}}") - 6;
                sb.Remove(selectionEnd, 6);
                cursor = selectionEnd;
            }
            else
            {
                selectionStart = cursor;
                selectionEnd = cursor;
            }

            // while (!System.Diagnostics.Debugger.IsAttached) System.Threading.Thread.Sleep(200);
            var completionTuple = CommandCompletion.MapStringInputToParsedInput(
                sb.ToString(),
                cursor);
            return new DocumentContext(
                (ScriptBlockAst)completionTuple.Item1,
                completionTuple.Item1.FindAstAt(completionTuple.Item3),
                new LinkedList<Token>(completionTuple.Item2).First.At(completionTuple.Item3),
                PositionUtilities.NewScriptExtent(
                    completionTuple.Item1.Extent,
                    selectionStart,
                    selectionEnd));
        }
    }
}
