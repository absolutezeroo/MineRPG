namespace MineRPG.RPG.Items;

/// <summary>
/// Data-driven item definition loaded from Data/Items/*.json.
/// Registered in an IRegistry&lt;int, ItemDefinition&gt;.
/// </summary>
public sealed class ItemDefinition
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public ItemRarity Rarity { get; init; }
    public int MaxStack { get; init; } = 64;
    public string? EquipmentSlot { get; init; }
    public string? LootTableRef { get; init; }
}
