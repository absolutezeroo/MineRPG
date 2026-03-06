namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when a player consumes an item (food, potion, etc.).
/// </summary>
public readonly struct ItemConsumedEvent
{
    /// <summary>The item definition ID that was consumed.</summary>
    public string ItemId { get; init; }

    /// <summary>Health restored by the consumable.</summary>
    public float HealthRestored { get; init; }

    /// <summary>Mana restored by the consumable.</summary>
    public float ManaRestored { get; init; }

    /// <summary>Hunger restored by the consumable.</summary>
    public float HungerRestored { get; init; }
}
