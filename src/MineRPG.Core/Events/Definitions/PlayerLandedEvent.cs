namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the player lands on the ground after being airborne.
/// Contains the total fall distance and any damage taken.
/// </summary>
public readonly struct PlayerLandedEvent
{
    /// <summary>Total vertical distance fallen in blocks.</summary>
    public float FallDistance { get; init; }

    /// <summary>Actual HP damage applied (0 if the fall was safe).</summary>
    public float DamageTaken { get; init; }
}
