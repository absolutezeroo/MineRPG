using Newtonsoft.Json;

namespace MineRPG.RPG.Items;

/// <summary>
/// Data-driven tool definition loaded from Data/Tools/*.json.
/// Defines mining type affinity, tier, and speed bonus.
/// </summary>
public sealed class ToolDefinition
{
    /// <summary>Unique numeric identifier for this tool.</summary>
    [JsonProperty("id")]
    public int Id { get; init; }

    /// <summary>Display name of the tool.</summary>
    [JsonProperty("name")]
    public string Name { get; init; } = "";

    /// <summary>
    /// Mining tool type category. Matches <see cref="MineRPG.World.Blocks.BlockDefinition.RequiredToolType"/>.
    /// Examples: "pickaxe", "axe", "shovel". Empty string means bare hands.
    /// </summary>
    [JsonProperty("toolType")]
    public string ToolType { get; init; } = "";

    /// <summary>
    /// Tier level of this tool. Higher tiers unlock harder blocks and mine faster.
    /// 0 = hand, 1 = wood, 2 = stone, 3 = iron, 4 = diamond.
    /// </summary>
    [JsonProperty("toolTier")]
    public int ToolTier { get; init; }

    /// <summary>
    /// Speed multiplier applied to mining time calculation.
    /// A value of 1.0 means no bonus over bare hands.
    /// </summary>
    [JsonProperty("speedMultiplier")]
    public float SpeedMultiplier { get; init; } = 1f;
}
