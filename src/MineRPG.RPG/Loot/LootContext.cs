namespace MineRPG.RPG.Loot;

/// <summary>
/// Context information for loot resolution, providing external modifiers.
/// </summary>
public readonly struct LootContext
{
    /// <summary>Default context with no modifiers.</summary>
    public static readonly LootContext Default;

    /// <summary>Looting enchantment level of the killing tool (0 if none).</summary>
    public int LootingLevel { get; init; }

    /// <summary>Luck modifier affecting drop chances (0.0 = no bonus).</summary>
    public float LuckModifier { get; init; }

    /// <summary>Biome identifier where the loot was generated.</summary>
    public string? BiomeId { get; init; }

    /// <summary>Whether the entity was killed by a player (vs environment).</summary>
    public bool IsPlayerKill { get; init; }
}
