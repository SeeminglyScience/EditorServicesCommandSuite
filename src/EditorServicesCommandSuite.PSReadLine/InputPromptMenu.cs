namespace EditorServicesCommandSuite.PSReadLine
{
    internal class InputPromptMenu : ConsoleBufferMenu<string>
    {
        internal InputPromptMenu(string caption, string message)
            : base(caption, message)
        {
        }

        protected override string GetResult()
        {
            return _input.ToString();
        }
    }
}
