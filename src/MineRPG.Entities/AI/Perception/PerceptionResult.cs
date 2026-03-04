namespace MineRPG.Entities.AI.Perception;

/// <summary>
/// A single perceived entity from a perception check.
/// </summary>
/// <param name="EntityId">Unique identifier of the perceived entity.</param>
/// <param name="Distance">Distance from the perceiver to the perceived entity.</param>
/// <param name="DirectionX">X component of the direction vector toward the entity.</param>
/// <param name="DirectionY">Y component of the direction vector toward the entity.</param>
/// <param name="DirectionZ">Z component of the direction vector toward the entity.</param>
/// <param name="IsVisible">Whether the entity is within the line of sight.</param>
public sealed record PerceptionResult(
    int EntityId,
    float Distance,
    float DirectionX,
    float DirectionY,
    float DirectionZ,
    bool IsVisible);
