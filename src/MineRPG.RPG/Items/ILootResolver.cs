namespace MineRPG.RPG.Items;

/// <summary>
/// Resolves a loot table reference into concrete item drops.
/// Implementations handle weighted random selection and conditions.
/// </summary>
public interface ILootResolver
{
    /// <summary>
    /// Resolves the specified loot table into a list of item drops.
    /// </summary>
    /// <param name="lootTableRef">The loot table reference identifier.</param>
    /// <param name="rng">Random number generator for weighted selection.</param>
    /// <returns>A read-only list of resolved item instances.</returns>
    public IReadOnlyList<ItemInstance> Resolve(string lootTableRef, Random rng);
}
