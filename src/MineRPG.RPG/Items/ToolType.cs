namespace MineRPG.RPG.Items;

/// <summary>
/// Types of tools that determine which blocks they mine efficiently.
/// </summary>
public enum ToolType
{
    /// <summary>Mines stone, ores, and similar hard blocks.</summary>
    Pickaxe,

    /// <summary>Chops wood and wooden blocks.</summary>
    Axe,

    /// <summary>Digs dirt, sand, gravel, and similar soft blocks.</summary>
    Shovel,

    /// <summary>Tills farmland and harvests crops.</summary>
    Hoe,

    /// <summary>Harvests leaves, wool, and similar materials.</summary>
    Shears,
}
