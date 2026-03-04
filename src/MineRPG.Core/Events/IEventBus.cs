using System;

namespace MineRPG.Core.Events;

/// <summary>
/// Thread-safe, typed pub/sub event bus for decoupled inter-system communication.
/// All events MUST be structs (value types). Handlers are called synchronously
/// on the publishing thread.
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// Subscribe a handler for events of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The event type (must be a struct).</typeparam>
    /// <param name="handler">The handler to invoke when the event is published.</param>
    void Subscribe<T>(Action<T> handler) where T : struct;

    /// <summary>
    /// Unsubscribe a previously registered handler.
    /// </summary>
    /// <typeparam name="T">The event type (must be a struct).</typeparam>
    /// <param name="handler">The handler to remove.</param>
    void Unsubscribe<T>(Action<T> handler) where T : struct;

    /// <summary>
    /// Dispatch <paramref name="eventData"/> to all current subscribers.
    /// Handlers are invoked in subscription order. Exceptions thrown by handlers
    /// are caught, logged, and do not interrupt remaining handlers.
    /// </summary>
    /// <typeparam name="T">The event type (must be a struct).</typeparam>
    /// <param name="eventData">The event data to publish.</param>
    void Publish<T>(T eventData) where T : struct;

    /// <summary>
    /// Buffer the event for later dispatch. Call <see cref="FlushQueued"/>
    /// on the main thread to process all queued events.
    /// Thread-safe: may be called from any thread.
    /// </summary>
    /// <typeparam name="T">The event type (must be a struct).</typeparam>
    /// <param name="eventData">The event data to queue.</param>
    void PublishQueued<T>(T eventData) where T : struct;

    /// <summary>
    /// Dispatch all queued events. Returns the number of events flushed.
    /// Should be called from the main thread (e.g., in _Process).
    /// </summary>
    /// <returns>The number of events that were flushed.</returns>
    int FlushQueued();

    /// <summary>
    /// Remove all subscribers and drain any queued events.
    /// </summary>
    void Clear();
}
