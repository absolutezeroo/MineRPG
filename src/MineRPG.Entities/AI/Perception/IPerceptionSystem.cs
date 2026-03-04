using System.Collections.Generic;

namespace MineRPG.Entities.AI.Perception;

/// <summary>
/// Evaluates what entities are visible or audible from a given position and orientation.
/// Runs at reduced frequency (0.2-0.5s) to limit performance impact.
/// </summary>
public interface IPerceptionSystem
{
    /// <summary>
    /// Performs a perception check from the given origin position and forward direction.
    /// </summary>
    /// <param name="originX">X coordinate of the perceiving entity.</param>
    /// <param name="originY">Y coordinate of the perceiving entity.</param>
    /// <param name="originZ">Z coordinate of the perceiving entity.</param>
    /// <param name="forwardX">X component of the forward direction vector.</param>
    /// <param name="forwardY">Y component of the forward direction vector.</param>
    /// <param name="forwardZ">Z component of the forward direction vector.</param>
    /// <param name="config">Perception configuration (sight range, hearing range, FOV).</param>
    /// <returns>A list of perceived entities within range.</returns>
    IReadOnlyList<PerceptionResult> Perceive(
        float originX, float originY, float originZ,
        float forwardX, float forwardY, float forwardZ,
        PerceptionData config);
}
