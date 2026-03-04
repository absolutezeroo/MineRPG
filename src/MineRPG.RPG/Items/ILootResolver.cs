namespace MineRPG.RPG.Items;

/// <summary>
/// Resolves a loot table reference into concrete item drops.
/// Implementations handle weighted random selection and conditions.
/// </summary>
public interface ILootResolver
{
    IReadOnlyList<ItemInstance> Resolve(string lootTableRef, Random rng);
}
