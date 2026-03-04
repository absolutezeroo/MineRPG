namespace MineRPG.Entities.AI.Spawning;

/// <summary>
/// Evaluates spawn rules against world conditions and produces spawn requests.
/// Does not instantiate Godot nodes — that is handled by the bridge layer.
/// </summary>
public interface ISpawner
{
    /// <summary>
    /// Evaluate which mobs should spawn near the given world position.
    /// Returns mob IDs and spawn positions.
    /// </summary>
    IReadOnlyList<SpawnRequest> Evaluate(
        float worldX, float worldY, float worldZ,
        string currentBiome,
        int lightLevel,
        Random rng);
}
