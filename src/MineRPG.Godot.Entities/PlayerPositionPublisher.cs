using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;

namespace MineRPG.Godot.Entities;

/// <summary>
/// Publishes <see cref="PlayerPositionUpdatedEvent"/> when the player moves
/// beyond a distance threshold. Avoids flooding the event bus with
/// micro-movements.
/// </summary>
internal sealed class PlayerPositionPublisher
{
    private const float PositionPublishThresholdSquared = 0.0001f;

    private readonly IEventBus _eventBus;

    private float _lastPublishedX;
    private float _lastPublishedY;
    private float _lastPublishedZ;

    /// <summary>
    /// Creates a position publisher for the given event bus.
    /// </summary>
    /// <param name="eventBus">The event bus to publish position events to.</param>
    public PlayerPositionPublisher(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    /// <summary>
    /// Seeds the tracking coordinates so the first real movement triggers a publish.
    /// </summary>
    /// <param name="x">Initial X position.</param>
    /// <param name="y">Initial Y position.</param>
    /// <param name="z">Initial Z position.</param>
    public void SeedPosition(float x, float y, float z)
    {
        _lastPublishedX = x;
        _lastPublishedY = y;
        _lastPublishedZ = z;
    }

    /// <summary>
    /// Publishes a position event if the player has moved beyond the threshold.
    /// </summary>
    /// <param name="x">Current X position.</param>
    /// <param name="y">Current Y position.</param>
    /// <param name="z">Current Z position.</param>
    public void PublishIfMoved(float x, float y, float z)
    {
        float deltaX = x - _lastPublishedX;
        float deltaY = y - _lastPublishedY;
        float deltaZ = z - _lastPublishedZ;

        if (deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ <= PositionPublishThresholdSquared)
        {
            return;
        }

        _lastPublishedX = x;
        _lastPublishedY = y;
        _lastPublishedZ = z;

        _eventBus.Publish(new PlayerPositionUpdatedEvent
        {
            X = x,
            Y = y,
            Z = z,
        });
    }
}
