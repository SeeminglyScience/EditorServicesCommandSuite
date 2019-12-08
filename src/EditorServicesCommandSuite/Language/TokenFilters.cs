namespace EditorServicesCommandSuite.Language
{
    internal delegate void TokenSearchStopper(in TokenNode node, ref bool stopSearch);

    internal delegate bool TokenFilter(in TokenNode node);

    internal delegate bool TokenFilter<TState>(in TokenNode node, TState state);
}
