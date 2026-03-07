using System.Collections.Generic;

using MineRPG.Core.Logging;
using MineRPG.RPG.Items;
using MineRPG.World.Blocks;

namespace MineRPG.Game.Bootstrap.Gameplay;

/// <summary>
/// Links block-category items to their corresponding block definitions by ID convention.
/// An item with ID "minerpg:dirt" and category Block is auto-linked to block "minerpg:dirt".
/// This replaces the deprecated PlacesBlockId JSON field.
/// Called once during bootstrap after both registries are loaded.
/// </summary>
public static class ItemBlockLinker
{
    /// <summary>
    /// Populates <see cref="ItemDefinition.LinkedBlockId"/> for all block-category items
    /// whose ID matches a registered block ID.
    /// </summary>
    /// <param name="itemRegistry">The item registry.</param>
    /// <param name="blockRegistry">The block registry.</param>
    /// <param name="logger">Logger for diagnostics and warnings.</param>
    public static void Link(ItemRegistry itemRegistry, BlockRegistry blockRegistry, ILogger logger)
    {
        IReadOnlyList<ItemDefinition> allItems = itemRegistry.GetAll();
        int linkedCount = 0;
        int warnCount = 0;

        for (int i = 0; i < allItems.Count; i++)
        {
            ItemDefinition item = allItems[i];

            if (item.Category != ItemCategory.Block)
            {
                continue;
            }

            if (blockRegistry.TryGet(item.Id, out BlockDefinition definition))
            {
                item.LinkedBlockId = definition.Id;
                linkedCount++;
            }
            else
            {
                logger.Warning(
                    "ItemBlockLinker: Block item '{0}' has no matching block in BlockRegistry.",
                    item.Id);
                warnCount++;
            }
        }

        logger.Info(
            "ItemBlockLinker: Linked {0} block items. {1} unresolved.",
            linkedCount, warnCount);
    }
}
