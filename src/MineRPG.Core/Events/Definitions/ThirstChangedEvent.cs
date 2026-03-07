namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the player's thirst changes meaningfully.
/// </summary>
public readonly struct ThirstChangedEvent
{
    /// <summary>New thirst value.</summary>
    public float NewValue { get; init; }

    /// <summary>Maximum thirst value.</summary>
    public float MaxValue { get; init; }
}
