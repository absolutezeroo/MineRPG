namespace MineRPG.Entities.AI.Perception;

/// <summary>
/// Evaluates what entities are visible or audible from a given position and orientation.
/// Runs at reduced frequency (0.2-0.5s) to limit performance impact.
/// </summary>
public interface IPerceptionSystem
{
    IReadOnlyList<PerceptionResult> Perceive(
        float originX, float originY, float originZ,
        float forwardX, float forwardY, float forwardZ,
        PerceptionData config);
}
