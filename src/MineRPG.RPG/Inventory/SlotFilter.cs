using MineRPG.RPG.Items;

namespace MineRPG.RPG.Inventory;

/// <summary>
/// Filter that restricts which items a slot can accept.
/// All conditions are ANDed: an item must pass every non-null filter.
/// </summary>
public sealed class SlotFilter
{
    /// <summary>A filter that accepts all items without restriction.</summary>
    public static readonly SlotFilter AcceptAll = new();

    /// <summary>
    /// Allowed item categories. Null means all categories are accepted.
    /// </summary>
    public IReadOnlyList<ItemCategory>? AllowedCategories { get; init; }

    /// <summary>
    /// Specific item IDs that are accepted. Null means all IDs are accepted.
    /// </summary>
    public IReadOnlyList<string>? AllowedItemIds { get; init; }

    /// <summary>
    /// Required tags. If set, the item must have at least one of these tags.
    /// Null means no tag requirement.
    /// </summary>
    public IReadOnlyList<string>? AllowedTags { get; init; }

    /// <summary>
    /// If set, only armor items with this slot type are accepted.
    /// </summary>
    public ArmorSlotType? RequiredArmorSlot { get; init; }

    /// <summary>
    /// Checks whether the given item definition passes this filter.
    /// </summary>
    /// <param name="definition">The item definition to check.</param>
    /// <returns>True if the item is accepted by this filter.</returns>
    public bool Accepts(ItemDefinition definition)
    {
        if (definition == null)
        {
            return false;
        }

        if (AllowedCategories != null)
        {
            bool categoryFound = false;

            for (int i = 0; i < AllowedCategories.Count; i++)
            {
                if (AllowedCategories[i] == definition.Category)
                {
                    categoryFound = true;
                    break;
                }
            }

            if (!categoryFound)
            {
                return false;
            }
        }

        if (AllowedItemIds != null)
        {
            bool idFound = false;

            for (int i = 0; i < AllowedItemIds.Count; i++)
            {
                if (AllowedItemIds[i] == definition.Id)
                {
                    idFound = true;
                    break;
                }
            }

            if (!idFound)
            {
                return false;
            }
        }

        if (AllowedTags != null)
        {
            bool tagFound = false;

            for (int i = 0; i < AllowedTags.Count; i++)
            {
                for (int j = 0; j < definition.Tags.Count; j++)
                {
                    if (AllowedTags[i] == definition.Tags[j])
                    {
                        tagFound = true;
                        break;
                    }
                }

                if (tagFound)
                {
                    break;
                }
            }

            if (!tagFound)
            {
                return false;
            }
        }

        if (RequiredArmorSlot.HasValue)
        {
            if (definition.Armor == null || definition.Armor.Slot != RequiredArmorSlot.Value)
            {
                return false;
            }
        }

        return true;
    }
}
