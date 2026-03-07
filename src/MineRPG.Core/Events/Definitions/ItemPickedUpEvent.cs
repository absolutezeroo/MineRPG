namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the player automatically picks up a dropped item from the world.
/// </summary>
public readonly struct ItemPickedUpEvent
{
    /// <summary>Definition ID of the item that was picked up.</summary>
    public string ItemDefinitionId { get; init; }

    /// <summary>Number of items collected.</summary>
    public int Count { get; init; }
}
