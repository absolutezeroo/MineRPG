using System;
using System.Collections.Generic;

using MineRPG.Core.DataLoading;
using MineRPG.Core.Logging;
using MineRPG.Core.Registry;

namespace MineRPG.RPG.Loot;

/// <summary>
/// Registry of loot table definitions, keyed by loot table ID.
/// Loads loot tables from JSON data directories at startup.
/// Supports loading from multiple directories (e.g., blocks, mobs).
/// </summary>
public sealed class LootTableRegistry
{
    private readonly Registry<string, LootTableDefinition> _inner = new();

    /// <summary>The underlying registry.</summary>
    public IRegistry<string, LootTableDefinition> Inner => _inner;

    /// <summary>Number of registered loot tables.</summary>
    public int Count => _inner.Count;

    /// <summary>
    /// Loads all loot table JSON files from the specified data subdirectory.
    /// Can be called multiple times for different directories before freezing.
    /// </summary>
    /// <param name="dataLoader">Data loader for reading JSON files.</param>
    /// <param name="subdirectory">The subdirectory under Data/ (e.g., "LootTables/Blocks").</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public void Load(IDataLoader dataLoader, string subdirectory, ILogger logger)
    {
        IReadOnlyList<LootTableDefinition> tables = dataLoader.LoadAll<LootTableDefinition>(subdirectory);

        foreach (LootTableDefinition table in tables)
        {
            if (string.IsNullOrEmpty(table.LootTableId))
            {
                logger.Warning(
                    "LootTableRegistry: Loot table with empty ID in '{0}' — skipping.",
                    subdirectory);
                continue;
            }

            _inner.Register(table.LootTableId, table);
        }

        logger.Info(
            "LootTableRegistry: Loaded {0} loot tables from '{1}'.",
            tables.Count, subdirectory);
    }

    /// <summary>
    /// Attempts to retrieve a loot table by its ID.
    /// </summary>
    /// <param name="lootTableId">The loot table identifier.</param>
    /// <param name="table">The found table, if any.</param>
    /// <returns>True if the table was found.</returns>
    public bool TryGet(string lootTableId, out LootTableDefinition table)
        => _inner.TryGet(lootTableId, out table!);

    /// <summary>
    /// Freezes the registry, preventing further modifications.
    /// </summary>
    public void Freeze() => _inner.Freeze();
}
