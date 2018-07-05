namespace EditorServicesCommandSuite.Internal
{
    public class DocumentEdit
    {
        private static int s_lastId = 1;

        public long StartOffset { get; set; }

        public long EndOffset { get; set; }

        public string OriginalValue { get; set; }

        public string NewValue { get; set; }

        internal int Id { get; } = s_lastId++;
    }
}
