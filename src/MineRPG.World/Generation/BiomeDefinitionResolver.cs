using System.Collections.Generic;

using MineRPG.Core.Logging;
using MineRPG.World.Biomes;
using MineRPG.World.Blocks;
using MineRPG.World.Generation.Ores;

namespace MineRPG.World.Generation;

/// <summary>
/// Resolves namespaced string block IDs in <see cref="BiomeDefinition"/> instances
/// and their <see cref="OreEntry"/> children to runtime ushort IDs.
/// Must be called once after <see cref="BlockRegistry"/> is fully loaded,
/// before any world generation begins.
/// </summary>
public static class BiomeDefinitionResolver
{
    /// <summary>
    /// Populates all runtime ushort block fields on the given biome definitions.
    /// Logs a warning for any unresolvable block ID and falls back to air (RuntimeId 0).
    /// </summary>
    /// <param name="biomes">Biome definitions to resolve.</param>
    /// <param name="blockRegistry">The fully loaded block registry.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public static void Resolve(
        IReadOnlyList<BiomeDefinition> biomes,
        BlockRegistry blockRegistry,
        ILogger logger)
    {
        for (int i = 0; i < biomes.Count; i++)
        {
            BiomeDefinition biome = biomes[i];

            biome.SurfaceBlock = ResolveBlock(
                biome.SurfaceBlockId, blockRegistry, logger, biome.Id, "surfaceBlock");
            biome.SubSurfaceBlock = ResolveBlock(
                biome.SubSurfaceBlockId, blockRegistry, logger, biome.Id, "subSurfaceBlock");
            biome.StoneBlock = ResolveBlock(
                biome.StoneBlockId, blockRegistry, logger, biome.Id, "stoneBlock");
            biome.UnderwaterBlock = ResolveBlock(
                biome.UnderwaterBlockId, blockRegistry, logger, biome.Id, "underwaterBlock");

            for (int j = 0; j < biome.Ores.Count; j++)
            {
                OreEntry ore = biome.Ores[j];
                ore.RuntimeBlockId = ResolveBlock(
                    ore.BlockId, blockRegistry, logger, biome.Id, "ore.block_id");
            }
        }

        logger.Info("BiomeDefinitionResolver: Resolved block IDs for {0} biomes.", biomes.Count);
    }

    /// <summary>
    /// Resolves ore definitions loaded from standalone ore data files.
    /// </summary>
    /// <param name="oreDefinitions">Ore definitions to resolve.</param>
    /// <param name="blockRegistry">The fully loaded block registry.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public static void ResolveOreDefinitions(
        IReadOnlyList<OreDefinition> oreDefinitions,
        BlockRegistry blockRegistry,
        ILogger logger)
    {
        for (int i = 0; i < oreDefinitions.Count; i++)
        {
            OreDefinition ore = oreDefinitions[i];
            ore.RuntimeBlockId = ResolveBlock(
                ore.BlockId, blockRegistry, logger, ore.Id, "block_id");
        }

        logger.Info("BiomeDefinitionResolver: Resolved block IDs for {0} ore definitions.", oreDefinitions.Count);
    }

    private static ushort ResolveBlock(
        string blockId,
        BlockRegistry blockRegistry,
        ILogger logger,
        string parentId,
        string fieldName)
    {
        if (string.IsNullOrEmpty(blockId))
        {
            return 0;
        }

        if (blockRegistry.TryGet(blockId, out BlockDefinition definition))
        {
            return definition.RuntimeId;
        }

        logger.Warning(
            "BiomeDefinitionResolver: '{0}' field '{1}' references unknown block '{2}'. Falling back to air.",
            parentId, fieldName, blockId);
        return 0;
    }
}
