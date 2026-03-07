namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the player's breath changes meaningfully.
/// </summary>
public readonly struct BreathChangedEvent
{
    /// <summary>New breath value (seconds of air).</summary>
    public float NewValue { get; init; }

    /// <summary>Maximum breath value.</summary>
    public float MaxValue { get; init; }
}
