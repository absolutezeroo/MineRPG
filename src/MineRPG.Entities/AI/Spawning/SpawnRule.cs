using System.Collections.Generic;

namespace MineRPG.Entities.AI.Spawning;

/// <summary>
/// Data-driven spawn conditions for a mob type. Loaded from Data/Mobs/*.json.
/// </summary>
public sealed class SpawnRule
{
    /// <summary>Maximum possible light level in the world.</summary>
    private const int DefaultMaxLightLevel = 15;

    /// <summary>Identifier of the mob type to spawn.</summary>
    public string MobId { get; init; } = "";

    /// <summary>Biomes in which this mob can spawn.</summary>
    public IReadOnlyList<string> Biomes { get; init; } = [];

    /// <summary>Minimum light level required for spawning.</summary>
    public int MinLightLevel { get; init; }

    /// <summary>Maximum light level allowed for spawning.</summary>
    public int MaxLightLevel { get; init; } = DefaultMaxLightLevel;

    /// <summary>Relative spawn weight compared to other rules.</summary>
    public float Weight { get; init; } = 1f;

    /// <summary>Minimum number of mobs to spawn in a group.</summary>
    public int MinGroupSize { get; init; } = 1;

    /// <summary>Maximum number of mobs to spawn in a group.</summary>
    public int MaxGroupSize { get; init; } = 1;
}
