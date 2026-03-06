using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

using InventoryContainer = MineRPG.RPG.Inventory.Inventory;

namespace MineRPG.RPG.Equipment;

/// <summary>
/// Manages the player's equipped armor and calculates combined equipment stats.
/// Detects and activates equipment set bonuses.
/// </summary>
public sealed class EquipmentManager
{
    private readonly InventoryContainer _armorInventory;
    private readonly ItemRegistry _itemRegistry;
    private readonly List<SetBonusDefinition> _setDefinitions;
    private readonly List<string> _activeResistances = new();

    private float _totalDefense;
    private float _totalToughness;
    private float _totalWeight;
    private bool _isDirty = true;
    private string? _activeSetId;
    private readonly List<SetBonus> _activeSetBonuses = new();

    /// <summary>
    /// Creates an equipment manager backed by the given armor inventory.
    /// </summary>
    /// <param name="armorInventory">The 4-slot filtered armor inventory.</param>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    /// <param name="setDefinitions">All known equipment set definitions.</param>
    public EquipmentManager(
        InventoryContainer armorInventory,
        ItemRegistry itemRegistry,
        IReadOnlyList<SetBonusDefinition> setDefinitions)
    {
        _armorInventory = armorInventory ?? throw new ArgumentNullException(nameof(armorInventory));
        _itemRegistry = itemRegistry ?? throw new ArgumentNullException(nameof(itemRegistry));
        _setDefinitions = new List<SetBonusDefinition>(setDefinitions ?? []);

        _armorInventory.SlotChanged += OnSlotChanged;
    }

    /// <summary>Combined defense from all equipped armor.</summary>
    public float TotalDefense
    {
        get
        {
            RecalculateIfDirty();
            return _totalDefense;
        }
    }

    /// <summary>Combined toughness from all equipped armor.</summary>
    public float TotalToughness
    {
        get
        {
            RecalculateIfDirty();
            return _totalToughness;
        }
    }

    /// <summary>Combined weight from all equipped armor.</summary>
    public float TotalWeight
    {
        get
        {
            RecalculateIfDirty();
            return _totalWeight;
        }
    }

    /// <summary>Movement speed modifier based on armor weight (1.0 = normal).</summary>
    public float MovementSpeedModifier
    {
        get
        {
            RecalculateIfDirty();
            float weightPenalty = _totalWeight * 0.01f;
            return Math.Max(0.5f, 1.0f - weightPenalty);
        }
    }

    /// <summary>All currently active damage resistances from equipped armor.</summary>
    public IReadOnlyList<string> ActiveResistances
    {
        get
        {
            RecalculateIfDirty();
            return _activeResistances;
        }
    }

    /// <summary>The currently active set ID, or null if no set is active.</summary>
    public string? ActiveSetId
    {
        get
        {
            RecalculateIfDirty();
            return _activeSetId;
        }
    }

    /// <summary>Currently active set bonuses.</summary>
    public IReadOnlyList<SetBonus> ActiveSetBonuses
    {
        get
        {
            RecalculateIfDirty();
            return _activeSetBonuses;
        }
    }

    /// <summary>Raised when equipment changes in any armor slot.</summary>
    public event EventHandler<EquipmentChangedEventArgs>? EquipmentChanged;

    /// <summary>Raised when equipment stats are recalculated.</summary>
    public event EventHandler? StatsRecalculated;

    /// <summary>
    /// Equips an armor item in the appropriate slot.
    /// </summary>
    /// <param name="slot">The armor slot to equip into.</param>
    /// <param name="armor">The armor item to equip.</param>
    /// <returns>The previously equipped item, or null.</returns>
    public ItemInstance? Equip(ArmorSlotType slot, ItemInstance armor)
    {
        int slotIndex = (int)slot;
        ItemInstance? previous = _armorInventory.GetSlot(slotIndex);

        _armorInventory.Slots[slotIndex].SetItem(armor);
        _isDirty = true;

        EquipmentChanged?.Invoke(this, new EquipmentChangedEventArgs(slot, previous, armor));

        return previous;
    }

    /// <summary>
    /// Unequips the armor in the specified slot.
    /// </summary>
    /// <param name="slot">The armor slot to unequip.</param>
    /// <returns>The unequipped item, or null if the slot was empty.</returns>
    public ItemInstance? Unequip(ArmorSlotType slot)
    {
        int slotIndex = (int)slot;
        ItemInstance? previous = _armorInventory.GetSlot(slotIndex);

        _armorInventory.Slots[slotIndex].SetItem(null);
        _isDirty = true;

        EquipmentChanged?.Invoke(this, new EquipmentChangedEventArgs(slot, previous, null));

        return previous;
    }

    /// <summary>
    /// Checks whether the given item can be equipped in the specified slot.
    /// </summary>
    /// <param name="slot">The target armor slot.</param>
    /// <param name="item">The item definition to check.</param>
    /// <returns>True if the item can be equipped in the slot.</returns>
    public static bool CanEquip(ArmorSlotType slot, ItemDefinition item)
    {
        if (item == null || item.Armor == null)
        {
            return false;
        }

        return item.Armor.Slot == slot;
    }

    private void OnSlotChanged(object? sender, SlotChangedEventArgs e) => _isDirty = true;

    private void RecalculateIfDirty()
    {
        if (!_isDirty)
        {
            return;
        }

        _isDirty = false;
        _totalDefense = 0f;
        _totalToughness = 0f;
        _totalWeight = 0f;
        _activeResistances.Clear();
        _activeSetBonuses.Clear();
        _activeSetId = null;

        List<string> equippedItemIds = new();

        for (int i = 0; i < _armorInventory.SlotCount; i++)
        {
            ItemInstance? armorItem = _armorInventory.GetSlot(i);

            if (armorItem == null)
            {
                continue;
            }

            if (!_itemRegistry.TryGet(armorItem.DefinitionId, out ItemDefinition definition))
            {
                continue;
            }

            equippedItemIds.Add(definition.Id);

            if (definition.Armor == null)
            {
                continue;
            }

            _totalDefense += definition.Armor.Defense;
            _totalToughness += definition.Armor.Toughness;
            _totalWeight += definition.Armor.Weight;

            for (int j = 0; j < definition.Armor.Resistances.Count; j++)
            {
                string resistance = definition.Armor.Resistances[j];

                if (!_activeResistances.Contains(resistance))
                {
                    _activeResistances.Add(resistance);
                }
            }
        }

        EvaluateSetBonuses(equippedItemIds);
        StatsRecalculated?.Invoke(this, EventArgs.Empty);
    }

    private void EvaluateSetBonuses(List<string> equippedItemIds)
    {
        for (int i = 0; i < _setDefinitions.Count; i++)
        {
            SetBonusDefinition setDef = _setDefinitions[i];
            int matchCount = 0;

            for (int j = 0; j < setDef.Pieces.Count; j++)
            {
                for (int k = 0; k < equippedItemIds.Count; k++)
                {
                    if (setDef.Pieces[j] == equippedItemIds[k])
                    {
                        matchCount++;
                        break;
                    }
                }
            }

            if (matchCount < 2)
            {
                continue;
            }

            _activeSetId = setDef.SetId;

            for (int j = 0; j < setDef.Bonuses.Count; j++)
            {
                if (matchCount >= setDef.Bonuses[j].RequiredPieces)
                {
                    _activeSetBonuses.Add(setDef.Bonuses[j]);
                }
            }

            break;
        }
    }
}
