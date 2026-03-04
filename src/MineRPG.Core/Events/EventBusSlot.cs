namespace MineRPG.Core.Events;

// Copy-on-write subscriber list for a single event type.
// The _snapshot field is replaced atomically; Publish reads it without locking.
internal sealed class EventBusSlot<T> : IEventBusSlot where T : struct
{
    private readonly Lock _writeLock = new();
    private Action<T>[] _snapshot = [];

    public void Add(Action<T> handler)
    {
        lock (_writeLock)
        {
            foreach (var existing in _snapshot)
                if (existing == handler)
                    return;

            var next = new Action<T>[_snapshot.Length + 1];
            Array.Copy(_snapshot, next, _snapshot.Length);
            next[_snapshot.Length] = handler;
            Volatile.Write(ref _snapshot, next);
        }
    }

    public void Remove(Action<T> handler)
    {
        lock (_writeLock)
        {
            var index = Array.IndexOf(_snapshot, handler);

            if (index < 0)
                return;

            var next = new Action<T>[_snapshot.Length - 1];
            Array.Copy(_snapshot, 0, next, 0, index);
            Array.Copy(_snapshot, index + 1, next, index, _snapshot.Length - index - 1);
            Volatile.Write(ref _snapshot, next);
        }
    }

    public Action<T>[] GetSnapshot() => Volatile.Read(ref _snapshot);
}
