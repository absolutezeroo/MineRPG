using MineRPG.RPG.Items;

namespace MineRPG.RPG.Loot;

/// <summary>
/// Resolves a loot table definition into concrete item drops using weighted random selection.
/// </summary>
public sealed class LootResolver : ILootResolver
{
    private readonly ItemRegistry _items;

    /// <summary>
    /// Creates a loot resolver with the given item registry.
    /// </summary>
    /// <param name="items">The item registry for creating item instances.</param>
    public LootResolver(ItemRegistry items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    /// <inheritdoc />
    public IReadOnlyList<ItemInstance> Resolve(string lootTableRef, Random rng)
    {
        return [];
    }

    /// <summary>
    /// Resolves a loot table definition into concrete item drops.
    /// </summary>
    /// <param name="table">The loot table definition to resolve.</param>
    /// <param name="context">Contextual modifiers affecting the loot.</param>
    /// <param name="rng">Random number generator for weighted selection.</param>
    /// <returns>A list of item instances generated from the table.</returns>
    public IReadOnlyList<ItemInstance> Resolve(
        LootTableDefinition table,
        LootContext context,
        Random rng)
    {
        if (table == null)
        {
            throw new ArgumentNullException(nameof(table));
        }

        if (rng == null)
        {
            throw new ArgumentNullException(nameof(rng));
        }

        if (table.Entries.Count == 0)
        {
            return [];
        }

        List<ItemInstance> results = new();

        int rollCount = rng.Next(table.MinRolls, table.MaxRolls + 1);
        rollCount += table.BonusRollsPerLootingLevel * context.LootingLevel;

        for (int roll = 0; roll < rollCount; roll++)
        {
            LootEntry? selected = SelectWeightedEntry(table.Entries, context, rng);

            if (selected == null || selected.IsEmpty)
            {
                continue;
            }

            int dropCount = rng.Next(selected.MinCount, selected.MaxCount + 1);

            if (dropCount <= 0)
            {
                continue;
            }

            if (!_items.Contains(selected.ItemId!))
            {
                continue;
            }

            ItemDefinition definition = _items.Get(selected.ItemId!);
            int durability = definition.HasDurability ? definition.MaxDurability : -1;

            ItemInstance drop = new ItemInstance(selected.ItemId!, dropCount, durability);
            results.Add(drop);
        }

        return results;
    }

    private static LootEntry? SelectWeightedEntry(
        IReadOnlyList<LootEntry> entries,
        LootContext context,
        Random rng)
    {
        // Filter eligible entries
        int totalWeight = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            if (AreConditionsMet(entries[i], context, rng))
            {
                totalWeight += entries[i].Weight;
            }
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = rng.Next(totalWeight);
        int cumulative = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            if (!AreConditionsMet(entries[i], context, rng))
            {
                continue;
            }

            cumulative += entries[i].Weight;

            if (roll < cumulative)
            {
                return entries[i];
            }
        }

        return null;
    }

    private static bool AreConditionsMet(
        LootEntry entry,
        LootContext context,
        Random rng)
    {
        for (int i = 0; i < entry.Conditions.Count; i++)
        {
            LootCondition condition = entry.Conditions[i];

            switch (condition.Type)
            {
                case "randomChance":
                    if (rng.NextDouble() > condition.Chance)
                    {
                        return false;
                    }

                    break;

                case "randomChanceWithLooting":
                    float adjustedChance = condition.Chance
                        + (condition.LootingMultiplier * context.LootingLevel);

                    if (rng.NextDouble() > adjustedChance)
                    {
                        return false;
                    }

                    break;

                case "playerKill":
                    if (!context.IsPlayerKill)
                    {
                        return false;
                    }

                    break;

                default:
                    break;
            }
        }

        return true;
    }
}
