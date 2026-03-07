namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the player's hunger or saturation changes meaningfully.
/// </summary>
public readonly struct HungerChangedEvent
{
    /// <summary>Current hunger points.</summary>
    public float Hunger { get; init; }

    /// <summary>Current saturation points.</summary>
    public float Saturation { get; init; }

    /// <summary>Maximum hunger value.</summary>
    public float MaxHunger { get; init; }
}
