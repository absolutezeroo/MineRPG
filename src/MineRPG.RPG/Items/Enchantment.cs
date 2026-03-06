namespace MineRPG.RPG.Items;

/// <summary>
/// An enchantment applied to an item instance, modifying its behavior or stats.
/// </summary>
public sealed class Enchantment
{
    /// <summary>Unique identifier of the enchantment type.</summary>
    public string EnchantmentId { get; init; } = "";

    /// <summary>Level of the enchantment (1 through 5).</summary>
    public int Level { get; init; } = 1;
}
