namespace MineRPG.Core.Events;

/// <summary>
/// Thread-safe, typed pub/sub event bus for decoupled inter-system communication.
/// All events MUST be structs (value types). Handlers are called synchronously
/// on the publishing thread.
/// </summary>
public interface IEventBus
{
    void Subscribe<T>(Action<T> handler) where T : struct;
    void Unsubscribe<T>(Action<T> handler) where T : struct;

    /// <summary>
    /// Dispatch <paramref name="eventData"/> to all current subscribers.
    /// Handlers are invoked in subscription order. Exceptions thrown by handlers
    /// are caught, logged, and do not interrupt remaining handlers.
    /// </summary>
    void Publish<T>(T eventData) where T : struct;

    /// <summary>
    /// Buffer the event for later dispatch. Call <see cref="FlushQueued"/>
    /// on the main thread to process all queued events.
    /// Thread-safe: may be called from any thread.
    /// </summary>
    void PublishQueued<T>(T eventData) where T : struct;

    /// <summary>
    /// Dispatch all queued events. Returns the number of events flushed.
    /// Should be called from the main thread (e.g., in _Process).
    /// </summary>
    int FlushQueued();

    void Clear();
}
