namespace MineRPG.RPG.Items;

/// <summary>
/// Runtime item instance with quantity and optional custom state.
/// </summary>
public sealed class ItemInstance
{
    public ItemDefinition Definition { get; }
    public int Quantity { get; set; }

    public ItemInstance(ItemDefinition definition, int quantity = 1)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Quantity = quantity;
    }
}
