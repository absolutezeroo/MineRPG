namespace MineRPG.Entities.AI.Perception;

/// <summary>
/// Configuration for an entity's perception capabilities.
/// </summary>
public sealed class PerceptionData
{
    public float SightRange { get; init; } = 16f;
    public float HearingRange { get; init; } = 8f;
    public float FieldOfView { get; init; } = 120f;
}
