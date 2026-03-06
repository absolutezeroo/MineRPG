namespace MineRPG.RPG.Items;

/// <summary>
/// Runtime instance of an item in an inventory slot.
/// Contains mutable state: quantity, durability, enchantments, and custom data.
/// </summary>
public sealed class ItemInstance
{
    private readonly List<Enchantment> _enchantments;

    /// <summary>
    /// Creates a new item instance referencing the given definition.
    /// </summary>
    /// <param name="definitionId">The ID of the <see cref="ItemDefinition"/> this instance is based on.</param>
    /// <param name="count">Initial stack count.</param>
    /// <param name="currentDurability">
    /// Current durability. Use -1 for items without durability.
    /// </param>
    public ItemInstance(string definitionId, int count = 1, int currentDurability = -1)
    {
        if (string.IsNullOrEmpty(definitionId))
        {
            throw new ArgumentException("Definition ID cannot be null or empty.", nameof(definitionId));
        }

        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be at least 1.");
        }

        DefinitionId = definitionId;
        Count = count;
        CurrentDurability = currentDurability;
        _enchantments = new List<Enchantment>();
        CustomData = new Dictionary<string, string>();
    }

    /// <summary>Reference to the item definition ID in the registry.</summary>
    public string DefinitionId { get; }

    /// <summary>Current stack count of this item instance.</summary>
    public int Count { get; set; }

    /// <summary>
    /// Current durability points remaining. -1 means the item has no durability.
    /// </summary>
    public int CurrentDurability { get; set; }

    /// <summary>Enchantments applied to this item instance.</summary>
    public IReadOnlyList<Enchantment> Enchantments => _enchantments;

    /// <summary>Arbitrary key-value data such as renamed items or custom properties.</summary>
    public Dictionary<string, string> CustomData { get; }

    /// <summary>Whether this item is broken (durability depleted).</summary>
    public bool IsBroken => HasDurability && CurrentDurability <= 0;

    /// <summary>Whether this item tracks durability.</summary>
    public bool HasDurability => CurrentDurability >= 0;

    /// <summary>
    /// Checks whether this item can stack with another item instance.
    /// Items can stack if they share the same definition, have no durability,
    /// and have identical enchantments and custom data.
    /// </summary>
    /// <param name="other">The other item instance to compare.</param>
    /// <returns>True if the items can be merged into one stack.</returns>
    public bool CanStackWith(ItemInstance other)
    {
        if (other == null)
        {
            return false;
        }

        if (DefinitionId != other.DefinitionId)
        {
            return false;
        }

        if (HasDurability || other.HasDurability)
        {
            return false;
        }

        if (_enchantments.Count != other._enchantments.Count)
        {
            return false;
        }

        for (int i = 0; i < _enchantments.Count; i++)
        {
            if (_enchantments[i].EnchantmentId != other._enchantments[i].EnchantmentId
                || _enchantments[i].Level != other._enchantments[i].Level)
            {
                return false;
            }
        }

        if (CustomData.Count != other.CustomData.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, string> pair in CustomData)
        {
            if (!other.CustomData.TryGetValue(pair.Key, out string? otherValue)
                || pair.Value != otherValue)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Splits off a number of items from this stack into a new instance.
    /// </summary>
    /// <param name="splitCount">Number of items to split off.</param>
    /// <returns>A new item instance with the split count.</returns>
    public ItemInstance Split(int splitCount)
    {
        if (splitCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(splitCount), splitCount, "Split count must be at least 1.");
        }

        if (splitCount >= Count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(splitCount), splitCount, "Split count must be less than current count.");
        }

        Count -= splitCount;

        ItemInstance splitInstance = new ItemInstance(DefinitionId, splitCount, CurrentDurability);

        for (int i = 0; i < _enchantments.Count; i++)
        {
            splitInstance._enchantments.Add(_enchantments[i]);
        }

        foreach (KeyValuePair<string, string> pair in CustomData)
        {
            splitInstance.CustomData[pair.Key] = pair.Value;
        }

        return splitInstance;
    }

    /// <summary>
    /// Merges another compatible item stack into this one.
    /// </summary>
    /// <param name="other">The item instance to merge from.</param>
    /// <param name="maxStackSize">Maximum stack size allowed for this item.</param>
    /// <returns>The number of items that could not be merged (overflow).</returns>
    public int Merge(ItemInstance other, int maxStackSize)
    {
        if (other == null)
        {
            throw new ArgumentNullException(nameof(other));
        }

        if (!CanStackWith(other))
        {
            return other.Count;
        }

        int spaceAvailable = maxStackSize - Count;
        int toTransfer = Math.Min(spaceAvailable, other.Count);

        Count += toTransfer;
        other.Count -= toTransfer;

        return other.Count;
    }

    /// <summary>
    /// Reduces the item's durability by the specified amount.
    /// </summary>
    /// <param name="amount">Amount of durability to remove.</param>
    public void DamageDurability(int amount)
    {
        if (!HasDurability)
        {
            return;
        }

        CurrentDurability = Math.Max(0, CurrentDurability - amount);
    }

    /// <summary>
    /// Restores the item's durability by the specified amount.
    /// </summary>
    /// <param name="amount">Amount of durability to restore.</param>
    /// <param name="maxDurability">Maximum durability this item can reach.</param>
    public void RepairDurability(int amount, int maxDurability)
    {
        if (!HasDurability)
        {
            return;
        }

        CurrentDurability = Math.Min(maxDurability, CurrentDurability + amount);
    }

    /// <summary>
    /// Adds an enchantment to this item instance.
    /// </summary>
    /// <param name="enchantment">The enchantment to add.</param>
    public void AddEnchantment(Enchantment enchantment)
    {
        if (enchantment == null)
        {
            throw new ArgumentNullException(nameof(enchantment));
        }

        _enchantments.Add(enchantment);
    }
}
