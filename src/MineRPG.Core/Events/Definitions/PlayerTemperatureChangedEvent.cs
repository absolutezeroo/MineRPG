namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the player's body temperature changes or crosses a comfort threshold.
/// </summary>
public readonly struct PlayerTemperatureChangedEvent
{
    /// <summary>Normalized body temperature in [-1, 1].</summary>
    public float NormalizedTemperature { get; init; }

    /// <summary>Whether the player is overheating.</summary>
    public bool IsOverheating { get; init; }

    /// <summary>Whether the player is freezing.</summary>
    public bool IsFreezing { get; init; }
}
