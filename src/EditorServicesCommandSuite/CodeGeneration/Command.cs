namespace EditorServicesCommandSuite.CodeGeneration
{
    internal sealed class Command
    {
        public Command(string name, object arguments = null)
        {
            Name = name;
            Arguments = arguments;
        }

        public string Name { get; }

        public object Arguments { get; }
    }
}
