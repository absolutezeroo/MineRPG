namespace MineRPG.RPG.Items;

/// <summary>
/// Item rarity tiers, used for loot generation and display color coding.
/// </summary>
public enum ItemRarity
{
    /// <summary>Basic items with no special properties.</summary>
    Common,

    /// <summary>Slightly better than common, minor bonus stats.</summary>
    Uncommon,

    /// <summary>Noticeably powerful items with meaningful bonuses.</summary>
    Rare,

    /// <summary>Highly powerful items, difficult to obtain.</summary>
    Epic,

    /// <summary>The rarest and most powerful tier of items.</summary>
    Legendary,
}
