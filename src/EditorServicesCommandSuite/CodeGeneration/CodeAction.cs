using System;
using System.Threading.Tasks;
using EditorServicesCommandSuite.Internal;

namespace EditorServicesCommandSuite.CodeGeneration
{
    internal delegate Task CodeActionFactory(DocumentContextBase context);

    internal delegate Task CodeActionFactory<TState>(DocumentContextBase context, TState state);

    internal abstract class CodeAction
    {
        public abstract string Id { get; }

        public abstract string Title { get; }

        internal virtual int Rank { get; }

        public static CodeAction Inactive(
            string id,
            string title,
            int rank = 0)
        {
            if (string.IsNullOrEmpty(title))
            {
                throw new ArgumentNullException(nameof(title));
            }

            return new DefaultCodeAction(
                id,
                title,
                rank,
                null);
        }

        public abstract Task ComputeChanges(DocumentContextBase context);

        internal CodeAction With(
            CodeActionFactory factory,
            string id = null,
            string title = null,
            int? rank = null)
        {
            return new DefaultCodeAction(
                id ?? Id,
                title ?? Title,
                rank ?? Rank,
                factory);
        }

        internal CodeAction With<TState>(
            CodeActionFactory<TState> factory,
            TState state,
            string id = null,
            string title = null,
            int? rank = null)
        {
            return new DefaultCodeAction<TState>(
                id ?? Id,
                title ?? Title,
                rank ?? Rank,
                factory,
                state);
        }

        internal CodeAction With<T, T1>(
            Func<DocumentContextBase, T, T1, Task> factory,
            (T, T1) state,
            string id = null,
            string title = null,
            int? rank = null)
        {
            return new DefaultCodeAction<(T arg0, T1 arg1)>(
                id ?? Id,
                title ?? Title,
                rank ?? Rank,
                (context, state) => factory(context, state.arg0, state.arg1),
                state);
        }

        internal CodeAction With<T, T1, T2>(
            Func<DocumentContextBase, T, T1, T2, Task> factory,
            (T, T1, T2) state,
            string id = null,
            string title = null,
            int? rank = null)
        {
            return new DefaultCodeAction<(T arg0, T1 arg1, T2 arg2)>(
                id ?? Id,
                title ?? Title,
                rank ?? Rank,
                (context, state) => factory(context, state.arg0, state.arg1, state.arg2),
                state);
        }

        internal CodeAction With<T, T1, T2, T3>(
            Func<DocumentContextBase, T, T1, T2, T3, Task> factory,
            (T, T1, T2, T3) state,
            string id = null,
            string title = null,
            int? rank = null)
        {
            return new DefaultCodeAction<(T arg0, T1 arg1, T2 arg2, T3 arg3)>(
                id ?? Id,
                title ?? Title,
                rank ?? Rank,
                (context, state) => factory(context, state.arg0, state.arg1, state.arg2, state.arg3),
                state);
        }

        internal CodeAction With<T, T1, T2, T3, T4>(
            Func<DocumentContextBase, T, T1, T2, T3, T4, Task> factory,
            (T, T1, T2, T3, T4) state,
            string id = null,
            string title = null,
            int? rank = null)
        {
            return new DefaultCodeAction<(T arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)>(
                id ?? Id,
                title ?? Title,
                rank ?? Rank,
                (context, state) => factory(
                    context,
                    state.arg0,
                    state.arg1,
                    state.arg2,
                    state.arg3,
                    state.arg4),
                state);
        }

        internal CodeAction With<T, T1, T2, T3, T4, T5>(
            Func<DocumentContextBase, T, T1, T2, T3, T4, T5, Task> factory,
            (T, T1, T2, T3, T4, T5) state,
            string id = null,
            string title = null,
            int? rank = null)
        {
            return new DefaultCodeAction<(T arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)>(
                id ?? Id,
                title ?? Title,
                rank ?? Rank,
                (context, state) => factory(
                    context,
                    state.arg0,
                    state.arg1,
                    state.arg2,
                    state.arg3,
                    state.arg4,
                    state.arg5),
                state);
        }

        private class DefaultCodeAction : CodeAction
        {
            private readonly CodeActionFactory _factory;

            public DefaultCodeAction(
                string id,
                string title,
                int rank,
                CodeActionFactory factory)
            {
                Id = id;
                Title = title;
                Rank = rank;
                _factory = factory;
            }

            public override string Title { get; }

            public override string Id { get; }

            internal override int Rank { get; }

            public override Task ComputeChanges(DocumentContextBase context)
            {
                return _factory(context);
            }
        }

        private class DefaultCodeAction<TState> : DefaultCodeAction
        {
            private readonly CodeActionFactory<TState> _factory;

            private readonly TState _state;

            public DefaultCodeAction(
                string id,
                string title,
                int rank,
                CodeActionFactory<TState> factory,
                TState state)
                : base(id, title, rank, null)
            {
                _factory = factory;
                _state = state;
            }

            public override Task ComputeChanges(DocumentContextBase context)
            {
                return _factory(context, _state);
            }
        }
    }
}
