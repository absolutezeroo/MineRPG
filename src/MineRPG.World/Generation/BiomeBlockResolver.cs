using System.Collections.Generic;

using MineRPG.Core.Logging;
using MineRPG.World.Biomes;
using MineRPG.World.Blocks;

namespace MineRPG.World.Generation;

/// <summary>
/// Resolves name-based block references in biome definitions to numeric IDs.
/// Falls back to the existing numeric ID when the name is null (backward compat).
/// </summary>
public static class BiomeBlockResolver
{
    /// <summary>
    /// Resolves all name-based block references for every biome definition.
    /// </summary>
    /// <param name="biomes">The biome definitions to resolve.</param>
    /// <param name="blockRegistry">The block registry for name lookups.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public static void ResolveAll(
        IReadOnlyList<BiomeDefinition> biomes,
        BlockRegistry blockRegistry,
        ILogger logger)
    {
        foreach (BiomeDefinition biome in biomes)
        {
            biome.SurfaceBlock = ResolveBlock(
                biome.SurfaceBlockName, biome.SurfaceBlock,
                biome.Id, "surfaceBlock", blockRegistry, logger);

            biome.SubSurfaceBlock = ResolveBlock(
                biome.SubSurfaceBlockName, biome.SubSurfaceBlock,
                biome.Id, "subSurfaceBlock", blockRegistry, logger);

            biome.StoneBlock = ResolveBlock(
                biome.StoneBlockName, biome.StoneBlock,
                biome.Id, "stoneBlock", blockRegistry, logger);

            biome.UnderwaterBlock = ResolveBlock(
                biome.UnderwaterBlockName, biome.UnderwaterBlock,
                biome.Id, "underwaterBlock", blockRegistry, logger);

            ResolveOreBlocks(biome, blockRegistry, logger);
        }
    }

    private static void ResolveOreBlocks(
        BiomeDefinition biome,
        BlockRegistry blockRegistry,
        ILogger logger)
    {
        for (int i = 0; i < biome.Ores.Count; i++)
        {
            OreEntry ore = biome.Ores[i];

            if (string.IsNullOrEmpty(ore.BlockName))
            {
                continue;
            }

            if (blockRegistry.TryGetByName(ore.BlockName, out BlockDefinition definition))
            {
                ore.BlockId = definition.Id;
            }
            else
            {
                logger.Warning(
                    "BiomeBlockResolver: Biome '{0}' ore references unknown block '{1}'.",
                    biome.Id, ore.BlockName);
            }
        }
    }

    private static ushort ResolveBlock(
        string? blockName,
        ushort fallbackId,
        string biomeId,
        string fieldName,
        BlockRegistry blockRegistry,
        ILogger logger)
    {
        if (blockName is null)
        {
            return fallbackId;
        }

        if (blockRegistry.TryGetByName(blockName, out BlockDefinition definition))
        {
            return definition.Id;
        }

        logger.Warning(
            "BiomeBlockResolver: Biome '{0}' references unknown block '{1}' for {2} - falling back to ID {3}.",
            biomeId, blockName, fieldName, fallbackId);
        return fallbackId;
    }
}
