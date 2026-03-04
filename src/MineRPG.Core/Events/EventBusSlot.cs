using System;
using System.Threading;

namespace MineRPG.Core.Events;

/// <summary>
/// Copy-on-write subscriber list for a single event type.
/// The _snapshot field is replaced atomically; Publish reads it without locking.
/// </summary>
internal sealed class EventBusSlot<T> : IEventBusSlot where T : struct
{
    private readonly Lock _writeLock = new();
    private Action<T>[] _snapshot = [];

    /// <summary>
    /// Add a handler to the subscriber list. Duplicate handlers are ignored.
    /// </summary>
    /// <param name="handler">The handler to add.</param>
    public void Add(Action<T> handler)
    {
        lock (_writeLock)
        {
            foreach (Action<T> existing in _snapshot)
            {
                if (existing == handler)
                {
                    return;
                }
            }

            Action<T>[] next = new Action<T>[_snapshot.Length + 1];
            Array.Copy(_snapshot, next, _snapshot.Length);
            next[_snapshot.Length] = handler;
            Volatile.Write(ref _snapshot, next);
        }
    }

    /// <summary>
    /// Remove a handler from the subscriber list.
    /// </summary>
    /// <param name="handler">The handler to remove.</param>
    public void Remove(Action<T> handler)
    {
        lock (_writeLock)
        {
            int index = Array.IndexOf(_snapshot, handler);

            if (index < 0)
            {
                return;
            }

            Action<T>[] next = new Action<T>[_snapshot.Length - 1];
            Array.Copy(_snapshot, 0, next, 0, index);
            Array.Copy(_snapshot, index + 1, next, index, _snapshot.Length - index - 1);
            Volatile.Write(ref _snapshot, next);
        }
    }

    /// <summary>
    /// Returns the current snapshot of handlers for lock-free iteration.
    /// </summary>
    /// <returns>The current handler array snapshot.</returns>
    public Action<T>[] GetSnapshot() => Volatile.Read(ref _snapshot);
}
