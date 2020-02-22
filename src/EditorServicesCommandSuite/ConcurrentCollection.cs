using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace EditorServicesCommandSuite
{
    internal sealed class ConcurrentCollection<T>
    {
        private readonly SemaphoreSlim _handle = new SemaphoreSlim(1, 1);

        private readonly List<T> _list;

        public ConcurrentCollection() => _list = new List<T>();

        public ConcurrentCollection(int capacity) => _list = new List<T>(capacity);

        public async Task AddAsync(T item, CancellationToken cancellationToken = default)
        {
            await _handle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _list.Add(item);
            }
            finally
            {
                _handle.Release();
            }
        }

        public async Task<ImmutableArray<T>> ToImmutableArray(CancellationToken cancellationToken = default)
        {
            await _handle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return _list.ToImmutableArray();
            }
            finally
            {
                _handle.Release();
            }
        }

        public async Task<T[]> ToArray(CancellationToken cancellationToken = default)
        {
            await _handle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return _list.ToArray();
            }
            finally
            {
                _handle.Release();
            }
        }
    }
}
