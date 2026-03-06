namespace MineRPG.RPG.Items;

/// <summary>
/// Material tiers for tools, determining durability and harvest level.
/// </summary>
public enum ToolMaterial : byte
{
    /// <summary>Lowest tier, basic tools.</summary>
    Wood,

    /// <summary>Second tier, slightly better than wood.</summary>
    Stone,

    /// <summary>Mid tier, reliable and affordable.</summary>
    Iron,

    /// <summary>Fast but fragile tier.</summary>
    Gold,

    /// <summary>High tier, strong and durable.</summary>
    Diamond,

    /// <summary>Highest tier, ultimate tools.</summary>
    Netherite,
}
