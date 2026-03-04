namespace MineRPG.Entities.AI.Spawning;

/// <summary>
/// A request to spawn a mob, produced by <see cref="ISpawner"/>.
/// The bridge layer converts this into a Godot node.
/// </summary>
public sealed record SpawnRequest(
    string MobId,
    float SpawnX,
    float SpawnY,
    float SpawnZ);
