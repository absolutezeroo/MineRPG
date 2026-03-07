using System;
using System.Collections.Generic;

using MineRPG.Core.Logging;
using MineRPG.Core.Registry;
using MineRPG.RPG.Items;
using MineRPG.RPG.Loot;
using MineRPG.World.Blocks;

namespace MineRPG.Game.Bootstrap.Validation;

/// <summary>
/// Cross-validates registries after all data has been loaded and frozen.
/// Logs errors for broken references (e.g. blocks referencing non-existent drop items).
/// Called once during <see cref="CompositionRoot.Wire"/>.
/// </summary>
public static class RegistryValidator
{
    /// <summary>
    /// Validates cross-references between the block, item, and tag registries.
    /// </summary>
    /// <param name="blockRegistry">The block registry (frozen).</param>
    /// <param name="itemRegistry">The item registry (frozen).</param>
    /// <param name="tagRegistry">The tag registry (frozen).</param>
    /// <param name="lootTableRegistry">The loot table registry (frozen).</param>
    /// <param name="logger">Logger for error and info output.</param>
    public static void Validate(
        BlockRegistry blockRegistry,
        ItemRegistry itemRegistry,
        TagRegistry tagRegistry,
        LootTableRegistry lootTableRegistry,
        ILogger logger)
    {
        int errorCount = 0;

        errorCount += ValidateBlockLootTableReferences(blockRegistry, lootTableRegistry, logger);
        errorCount += ValidateLootTableEntryReferences(lootTableRegistry, itemRegistry, logger);
        errorCount += ValidateTagReferences(tagRegistry, logger);
        errorCount += ValidateToolEffectiveOnTags(itemRegistry, tagRegistry, logger);

        if (errorCount > 0)
        {
            logger.Warning(
                "RegistryValidator: Completed with {0} validation error(s).", errorCount);
        }
        else
        {
            logger.Info("RegistryValidator: All cross-references valid.");
        }
    }

    private static int ValidateBlockLootTableReferences(
        BlockRegistry blockRegistry,
        LootTableRegistry lootTableRegistry,
        ILogger logger)
    {
        int errorCount = 0;
        IReadOnlyList<BlockDefinition> blocks = blockRegistry.Inner.GetAll();

        for (int i = 0; i < blocks.Count; i++)
        {
            BlockDefinition block = blocks[i];

            if (string.IsNullOrEmpty(block.LootTableId))
            {
                continue;
            }

            if (!lootTableRegistry.TryGet(block.LootTableId, out _))
            {
                logger.Error(
                    "RegistryValidator: Block '{0}' references unknown loot table '{1}'.",
                    block.DisplayName, block.LootTableId);
                errorCount++;
            }
        }

        return errorCount;
    }

    private static int ValidateLootTableEntryReferences(
        LootTableRegistry lootTableRegistry,
        ItemRegistry itemRegistry,
        ILogger logger)
    {
        int errorCount = 0;
        IReadOnlyList<LootTableDefinition> tables = lootTableRegistry.Inner.GetAll();

        for (int i = 0; i < tables.Count; i++)
        {
            LootTableDefinition table = tables[i];

            for (int j = 0; j < table.Entries.Count; j++)
            {
                LootEntry entry = table.Entries[j];

                if (entry.IsEmpty)
                {
                    continue;
                }

                if (!itemRegistry.Contains(entry.ItemId!))
                {
                    logger.Error(
                        "RegistryValidator: LootTable '{0}' entry {1} references unknown item '{2}'.",
                        table.LootTableId, j, entry.ItemId);
                    errorCount++;
                }
            }
        }

        return errorCount;
    }

    private static int ValidateTagReferences(TagRegistry tagRegistry, ILogger logger)
    {
        int errorCount = 0;

        foreach (TagDefinition tag in tagRegistry.GetAll())
        {
            if (tag.Values.Count == 0)
            {
                logger.Warning(
                    "RegistryValidator: Tag '{0}' has no values.", tag.TagId);
            }

            for (int i = 0; i < tag.Values.Count; i++)
            {
                string value = tag.Values[i];

                if (string.IsNullOrWhiteSpace(value))
                {
                    logger.Error(
                        "RegistryValidator: Tag '{0}' contains an empty value at index {1}.",
                        tag.TagId, i);
                    errorCount++;
                }
            }
        }

        return errorCount;
    }

    private static int ValidateToolEffectiveOnTags(
        ItemRegistry itemRegistry,
        TagRegistry tagRegistry,
        ILogger logger)
    {
        int errorCount = 0;
        IReadOnlyList<ItemDefinition> items = itemRegistry.GetAll();

        for (int i = 0; i < items.Count; i++)
        {
            ItemDefinition item = items[i];

            if (item.Tool is null)
            {
                continue;
            }

            IReadOnlyList<string> effectiveOn = item.Tool.EffectiveOn;

            for (int j = 0; j < effectiveOn.Count; j++)
            {
                string reference = effectiveOn[j];

                if (TagRegistry.IsTagReference(reference))
                {
                    string tagId = TagRegistry.StripTagPrefix(reference);

                    if (!tagRegistry.TryGet(tagId, out _))
                    {
                        logger.Error(
                            "RegistryValidator: Item '{0}' effectiveOn references unknown tag '{1}'.",
                            item.Id, reference);
                        errorCount++;
                    }
                }
            }
        }

        return errorCount;
    }
}
