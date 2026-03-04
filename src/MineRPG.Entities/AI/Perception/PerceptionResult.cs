namespace MineRPG.Entities.AI.Perception;

/// <summary>
/// A single perceived entity from a perception check.
/// </summary>
public sealed record PerceptionResult(
    int EntityId,
    float Distance,
    float DirectionX,
    float DirectionY,
    float DirectionZ,
    bool IsVisible);
