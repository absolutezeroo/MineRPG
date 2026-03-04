namespace MineRPG.Entities.AI.Perception;

/// <summary>
/// Configuration for an entity's perception capabilities.
/// </summary>
public sealed class PerceptionData
{
    /// <summary>Maximum range at which the entity can see other entities.</summary>
    private const float DefaultSightRange = 16f;

    /// <summary>Maximum range at which the entity can hear other entities.</summary>
    private const float DefaultHearingRange = 8f;

    /// <summary>Field of view angle in degrees.</summary>
    private const float DefaultFieldOfView = 120f;

    /// <summary>Maximum distance at which the entity can see targets, in blocks.</summary>
    public float SightRange { get; init; } = DefaultSightRange;

    /// <summary>Maximum distance at which the entity can hear targets, in blocks.</summary>
    public float HearingRange { get; init; } = DefaultHearingRange;

    /// <summary>Cone of vision angle in degrees.</summary>
    public float FieldOfView { get; init; } = DefaultFieldOfView;
}
