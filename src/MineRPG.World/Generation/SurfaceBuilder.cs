using System;
using System.Runtime.CompilerServices;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation;

/// <summary>
/// Applies biome-specific surface blocks to terrain columns.
/// Determines top block, filler block, and underwater block based on the biome definition.
/// Thread-safe: stateless, all inputs are provided per call.
/// </summary>
public sealed class SurfaceBuilder
{
    private const int SeaLevel = 62;

    private readonly ushort _waterBlockId;
    private readonly ushort _bedrockBlockId;
    private readonly ushort _defaultStoneId;

    /// <summary>
    /// Creates a surface builder with the given block IDs.
    /// </summary>
    /// <param name="waterBlockId">Block ID for water.</param>
    /// <param name="bedrockBlockId">Block ID for bedrock.</param>
    /// <param name="defaultStoneId">Block ID for default stone.</param>
    public SurfaceBuilder(ushort waterBlockId, ushort bedrockBlockId, ushort defaultStoneId)
    {
        _waterBlockId = waterBlockId;
        _bedrockBlockId = bedrockBlockId;
        _defaultStoneId = defaultStoneId;
    }

    /// <summary>
    /// Fills a single column of a chunk with appropriate blocks based on the biome.
    /// </summary>
    /// <param name="data">The chunk data to modify.</param>
    /// <param name="localX">Local X coordinate within the chunk.</param>
    /// <param name="localZ">Local Z coordinate within the chunk.</param>
    /// <param name="surfaceY">The surface height at this column.</param>
    /// <param name="biome">The biome definition for this column.</param>
    public void BuildColumn(
        ChunkData data,
        int localX,
        int localZ,
        int surfaceY,
        BiomeDefinition biome)
    {
        for (int y = 0; y < ChunkData.SizeY; y++)
        {
            ushort blockId = DetermineBlock(y, surfaceY, biome);
            data.SetBlock(localX, y, localZ, blockId);
        }
    }

    /// <summary>
    /// Fills a single column with blended biome blocks at a boundary.
    /// </summary>
    /// <param name="data">The chunk data to modify.</param>
    /// <param name="localX">Local X coordinate within the chunk.</param>
    /// <param name="localZ">Local Z coordinate within the chunk.</param>
    /// <param name="surfaceY">The surface height at this column.</param>
    /// <param name="primaryBiome">Primary biome for this column.</param>
    /// <param name="secondaryBiome">Secondary biome for blending.</param>
    /// <param name="blendWeight">Blend weight (0 = primary, 1 = secondary).</param>
    public void BuildBlendedColumn(
        ChunkData data,
        int localX,
        int localZ,
        int surfaceY,
        BiomeDefinition primaryBiome,
        BiomeDefinition secondaryBiome,
        float blendWeight)
    {
        for (int y = 0; y < ChunkData.SizeY; y++)
        {
            ushort blockId = DetermineBlendedBlock(y, surfaceY, primaryBiome, secondaryBiome, blendWeight);
            data.SetBlock(localX, y, localZ, blockId);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort DetermineBlock(int y, int surfaceY, BiomeDefinition biome)
    {
        if (y == 0)
        {
            return _bedrockBlockId;
        }

        if (y > surfaceY)
        {
            return y <= SeaLevel && _waterBlockId != 0 ? _waterBlockId : (ushort)0;
        }

        if (y == surfaceY)
        {
            if (surfaceY <= SeaLevel && _waterBlockId != 0)
            {
                return biome.UnderwaterBlock != 0 ? biome.UnderwaterBlock : biome.SubSurfaceBlock;
            }

            return biome.SurfaceBlock;
        }

        if (y > surfaceY - biome.SubSurfaceDepth)
        {
            return biome.SubSurfaceBlock;
        }

        return biome.StoneBlock != 0 ? biome.StoneBlock : _defaultStoneId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ushort DetermineBlendedBlock(
        int y,
        int surfaceY,
        BiomeDefinition primaryBiome,
        BiomeDefinition secondaryBiome,
        float blendWeight)
    {
        if (y == 0)
        {
            return _bedrockBlockId;
        }

        if (y > surfaceY)
        {
            return y <= SeaLevel && _waterBlockId != 0 ? _waterBlockId : (ushort)0;
        }

        if (y == surfaceY)
        {
            if (surfaceY <= SeaLevel && _waterBlockId != 0)
            {
                ushort primary = primaryBiome.UnderwaterBlock != 0
                    ? primaryBiome.UnderwaterBlock : primaryBiome.SubSurfaceBlock;
                ushort secondary = secondaryBiome.UnderwaterBlock != 0
                    ? secondaryBiome.UnderwaterBlock : secondaryBiome.SubSurfaceBlock;
                return BlendBlock(primary, secondary, blendWeight);
            }

            return BlendBlock(primaryBiome.SurfaceBlock, secondaryBiome.SurfaceBlock, blendWeight);
        }

        int maxSubDepth = Math.Max(primaryBiome.SubSurfaceDepth, secondaryBiome.SubSurfaceDepth);

        if (y > surfaceY - maxSubDepth)
        {
            return BlendBlock(primaryBiome.SubSurfaceBlock, secondaryBiome.SubSurfaceBlock, blendWeight);
        }

        ushort primaryStone = primaryBiome.StoneBlock != 0 ? primaryBiome.StoneBlock : _defaultStoneId;
        return primaryStone;
    }

    private const float BlendThreshold = 0.5f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort BlendBlock(ushort primary, ushort secondary, float blendWeight)
        => blendWeight < BlendThreshold ? primary : secondary;
}
