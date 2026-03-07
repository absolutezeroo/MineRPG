namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published after the player respawns following death.
/// The bridge uses this to teleport the CharacterBody3D.
/// </summary>
public readonly struct PlayerRespawnedEvent
{
    /// <summary>Spawn X position.</summary>
    public float SpawnX { get; init; }

    /// <summary>Spawn Y position.</summary>
    public float SpawnY { get; init; }

    /// <summary>Spawn Z position.</summary>
    public float SpawnZ { get; init; }
}
