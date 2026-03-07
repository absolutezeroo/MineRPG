using System;

using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Math;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;

namespace MineRPG.Game.Bootstrap.Gameplay;

/// <summary>
/// Implements <see cref="ITerrainQuery"/> by scanning loaded chunk data
/// from the top down to find the highest solid block at a given XZ column.
/// Lives in MineRPG.Game where both World and Core references are available.
/// </summary>
public sealed class ChunkTerrainQuery : ITerrainQuery
{
    private const float DefaultFallbackY = -1f;

    private readonly IChunkManager _chunkManager;
    private readonly BlockRegistry _blockRegistry;

    /// <summary>
    /// Creates a terrain query backed by the chunk manager and block registry.
    /// </summary>
    /// <param name="chunkManager">The chunk manager holding loaded chunks.</param>
    /// <param name="blockRegistry">The block registry for solidity lookups.</param>
    public ChunkTerrainQuery(IChunkManager chunkManager, BlockRegistry blockRegistry)
    {
        _chunkManager = chunkManager;
        _blockRegistry = blockRegistry;
    }

    /// <inheritdoc />
    public float FallbackY => DefaultFallbackY;

    /// <inheritdoc />
    public float GetSurfaceY(float worldX, float worldZ)
    {
        int blockX = (int)MathF.Floor(worldX);
        int blockZ = (int)MathF.Floor(worldZ);

        ChunkCoord2D chunkCoord2D = VoxelMath.WorldToChunk(
            blockX, blockZ, ChunkData.SizeX, ChunkData.SizeZ);

        ChunkCoord coord = new(chunkCoord2D.ChunkX, chunkCoord2D.ChunkZ);

        if (!_chunkManager.TryGet(coord, out ChunkEntry? entry) || entry?.Data is null)
        {
            return DefaultFallbackY;
        }

        LocalCoord2D localCoord = VoxelMath.WorldToLocal(
            blockX, blockZ, ChunkData.SizeX, ChunkData.SizeZ);

        ChunkData data = entry.Data;

        for (int y = ChunkData.SizeY - 1; y >= 0; y--)
        {
            ushort blockId = data.GetBlock(localCoord.LocalX, y, localCoord.LocalZ);

            if (blockId == 0)
            {
                continue;
            }

            BlockDefinition block = _blockRegistry.Get(blockId);

            if (block.IsSolid)
            {
                // Return the top face of the block (block Y + 1)
                return y + 1f;
            }
        }

        return DefaultFallbackY;
    }
}
