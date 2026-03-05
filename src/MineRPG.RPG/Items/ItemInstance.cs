namespace MineRPG.RPG.Items;

/// <summary>
/// Runtime item instance with quantity and optional custom state.
/// </summary>
public sealed class ItemInstance
{
    /// <summary>
    /// Creates a new item instance from the given definition.
    /// </summary>
    /// <param name="definition">The item definition this instance is based on.</param>
    /// <param name="quantity">The initial stack quantity.</param>
    public ItemInstance(ItemDefinition definition, int quantity = 1)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
        Quantity = quantity;
    }

    /// <summary>The item definition this instance is based on.</summary>
    public ItemDefinition Definition { get; }

    /// <summary>Current stack quantity of this item instance.</summary>
    public int Quantity { get; set; }
}
