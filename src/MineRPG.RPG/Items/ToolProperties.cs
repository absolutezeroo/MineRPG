namespace MineRPG.RPG.Items;

/// <summary>
/// Properties specific to tool items (pickaxe, axe, shovel, etc.).
/// Loaded from JSON as part of an <see cref="ItemDefinition"/>.
/// </summary>
public sealed class ToolProperties
{
    /// <summary>The type of tool determining which blocks it mines efficiently.</summary>
    public ToolType ToolType { get; init; }

    /// <summary>The material tier of the tool.</summary>
    public ToolMaterial Material { get; init; }

    /// <summary>Mining speed multiplier applied when mining compatible blocks.</summary>
    public float MiningSpeed { get; init; }

    /// <summary>
    /// Harvest level determining which blocks can be mined.
    /// 0=wood, 1=stone, 2=iron, 3=diamond, 4=netherite.
    /// </summary>
    public int HarvestLevel { get; init; }

    /// <summary>Block IDs this tool is particularly effective on.</summary>
    public IReadOnlyList<string> EffectiveOn { get; init; } = [];
}
