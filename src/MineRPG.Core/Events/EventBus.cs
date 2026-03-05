using System;
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
public sealed class EventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, IEventBusSlot> _slots = new();
    private readonly ConcurrentQueue<Action> _deferredQueue = new();
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="EventBus"/>.
    /// </summary>
    /// <param name="logger">Logger for recording handler errors.</param>
    public EventBus(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Subscribe a handler for events of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="handler">The handler to invoke when the event is published.</param>
    public void Subscribe<T>(Action<T> handler) where T : struct => GetOrCreateSlot<T>().Add(handler);

    /// <summary>
    /// Unsubscribe a previously registered handler for events of type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="handler">The handler to remove.</param>
    public void Unsubscribe<T>(Action<T> handler) where T : struct
    {
        if (_slots.TryGetValue(typeof(T), out IEventBusSlot? slot))
        {
            ((EventBusSlot<T>)slot).Remove(handler);
        }
    }

    /// <inheritdoc />
    public void Publish<T>(T eventData) where T : struct
    {
        if (!_slots.TryGetValue(typeof(T), out IEventBusSlot? slot))
        {
            return;
        }

        Action<T>[] handlers = ((EventBusSlot<T>)slot).GetSnapshot();

        foreach (Action<T> handler in handlers)
        {
            try
            {
                handler(eventData);
            }
            catch (Exception ex)
            {
                _logger.Error("EventBus handler threw for event {0}: {1}", ex, typeof(T).Name, ex.Message);
            }
        }
    }

    /// <inheritdoc />
    public void PublishQueued<T>(T eventData) where T : struct => _deferredQueue.Enqueue(() => Publish(eventData));

    /// <inheritdoc />
    public int FlushQueued()
    {
        int count = 0;

        while (_deferredQueue.TryDequeue(out Action? action))
        {
            action();
            count++;
        }

        return count;
    }

    /// <inheritdoc />
    public void Clear()
    {
        _slots.Clear();

        // Drain deferred queue to prevent stale events from firing later
        while (_deferredQueue.TryDequeue(out _))
        {
        }
    }

    private EventBusSlot<T> GetOrCreateSlot<T>() where T : struct => (EventBusSlot<T>)_slots.GetOrAdd(typeof(T), _ => new EventBusSlot<T>());
}
