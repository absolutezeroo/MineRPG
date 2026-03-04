using System.Collections.Generic;

using Newtonsoft.Json;

namespace MineRPG.World.Biomes;

/// <summary>
/// Gameplay-related settings for a biome (mob spawns, difficulty, loot).
/// </summary>
public sealed class BiomeGameplay
{
    /// <summary>References to mob spawn rules active in this biome.</summary>
    [JsonProperty("mob_spawn_table")]
    public IReadOnlyList<string> MobSpawnTable { get; init; } = [];

    /// <summary>Difficulty multiplier for mobs in this biome.</summary>
    [JsonProperty("difficulty_modifier")]
    public float DifficultyModifier { get; init; } = 1.0f;

    /// <summary>Override loot tables specific to this biome.</summary>
    [JsonProperty("loot_table_overrides")]
    public IReadOnlyList<string> LootTableOverrides { get; init; } = [];
}
