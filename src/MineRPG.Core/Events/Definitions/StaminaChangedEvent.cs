namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the player's stamina changes meaningfully.
/// </summary>
public readonly struct StaminaChangedEvent
{
    /// <summary>New stamina value.</summary>
    public float NewValue { get; init; }

    /// <summary>Maximum stamina value.</summary>
    public float MaxValue { get; init; }
}
