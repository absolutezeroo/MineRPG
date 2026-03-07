namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the player's health changes by a meaningful threshold.
/// </summary>
public readonly struct HealthChangedEvent
{
    /// <summary>New health value.</summary>
    public float NewValue { get; init; }

    /// <summary>Maximum health value.</summary>
    public float MaxValue { get; init; }
}
