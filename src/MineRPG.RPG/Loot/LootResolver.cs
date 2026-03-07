using MineRPG.RPG.Items;

namespace MineRPG.RPG.Loot;

/// <summary>
/// Resolves a loot table definition into concrete item drops using weighted random selection.
/// </summary>
public sealed class LootResolver : ILootResolver
{
    private readonly ItemRegistry _items;
    private readonly LootTableRegistry _lootTables;

    /// <summary>
    /// Creates a loot resolver with the given registries.
    /// </summary>
    /// <param name="items">The item registry for creating item instances.</param>
    /// <param name="lootTables">The loot table registry for looking up tables by ID.</param>
    public LootResolver(ItemRegistry items, LootTableRegistry lootTables)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _lootTables = lootTables ?? throw new ArgumentNullException(nameof(lootTables));
    }

    /// <inheritdoc />
    public IReadOnlyList<ItemInstance> Resolve(string lootTableRef, Random rng)
    {
        if (string.IsNullOrEmpty(lootTableRef))
        {
            return [];
        }

        if (!_lootTables.TryGet(lootTableRef, out LootTableDefinition table))
        {
            return [];
        }

        return Resolve(table, LootContext.Default, rng);
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
        // Evaluate conditions once and cache results to avoid consuming
        // RNG state twice (randomChance conditions use rng.NextDouble()).
        Span<bool> eligible = entries.Count <= 64
            ? stackalloc bool[entries.Count]
            : new bool[entries.Count];

        int totalWeight = 0;

        for (int i = 0; i < entries.Count; i++)
        {
            eligible[i] = AreConditionsMet(entries[i], context, rng);

            if (eligible[i])
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
            if (!eligible[i])
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
                    throw new InvalidOperationException(
                        $"Unknown loot condition type '{condition.Type}'");
            }
        }

        return true;
    }
}
