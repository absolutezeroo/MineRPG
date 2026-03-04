namespace MineRPG.Entities.AI.Spawning;

/// <summary>
/// A request to spawn a mob, produced by <see cref="ISpawner"/>.
/// The bridge layer converts this into a Godot node.
/// </summary>
/// <param name="MobId">Identifier of the mob type to spawn.</param>
/// <param name="SpawnX">X coordinate in world space for the spawn location.</param>
/// <param name="SpawnY">Y coordinate in world space for the spawn location.</param>
/// <param name="SpawnZ">Z coordinate in world space for the spawn location.</param>
public sealed record SpawnRequest(
    string MobId,
    float SpawnX,
    float SpawnY,
    float SpawnZ);
