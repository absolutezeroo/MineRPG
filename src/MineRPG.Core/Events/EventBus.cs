using System.Collections.Concurrent;
using MineRPG.Core.Logging;

namespace MineRPG.Core.Events;

/// <summary>
/// Default implementation of <see cref="IEventBus"/>.
///
/// Thread-safety model:
/// - Per-type subscriber lists use copy-on-write snapshots.
/// - Publish reads the snapshot without holding any lock.
/// - Subscribe/Unsubscribe lock only the per-type slot.
/// - PublishQueued buffers events in a ConcurrentQueue for later flush.
/// </summary>
public sealed class EventBus(ILogger logger) : IEventBus
{
    private readonly ConcurrentDictionary<Type, IEventBusSlot> _slots = new();
    private readonly ConcurrentQueue<Action> _deferredQueue = new();

    public void Subscribe<T>(Action<T> handler) where T : struct => GetOrCreateSlot<T>().Add(handler);

    public void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        if (_slots.TryGetValue(typeof(T), out var slot))
            ((EventBusSlot<T>)slot).Remove(handler);
    }

    public void Publish<T>(T eventData) where T : struct
    {
        if (!_slots.TryGetValue(typeof(T), out var slot))
            return;

        var handlers = ((EventBusSlot<T>)slot).GetSnapshot();

        foreach (var handler in handlers)
        {
            try
            {
                handler(eventData);
            }
            catch (Exception ex)
            {
                logger.Error("EventBus handler threw for event {0}: {1}", ex, typeof(T).Name, ex.Message);
            }
        }
    }

    public void PublishQueued<T>(T eventData) where T : struct
        => _deferredQueue.Enqueue(() => Publish(eventData));

    public int FlushQueued()
    {
        var count = 0;
        while (_deferredQueue.TryDequeue(out var action))
        {
            action();
            count++;
        }

        return count;
    }

    public void Clear()
    {
        _slots.Clear();

        // Drain deferred queue to prevent stale events from firing later
        while (_deferredQueue.TryDequeue(out _)) { }
    }

    private EventBusSlot<T> GetOrCreateSlot<T>() where T : struct
        => (EventBusSlot<T>)_slots.GetOrAdd(typeof(T), _ => new EventBusSlot<T>());
}
