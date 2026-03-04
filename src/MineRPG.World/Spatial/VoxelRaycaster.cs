using System;

using MineRPG.Core.Math;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;

namespace MineRPG.World.Spatial;

/// <summary>
/// DDA (Digital Differential Analysis) voxel raycast.
/// Operates directly on ChunkData -- does not use Godot's physics engine.
/// </summary>
public sealed class VoxelRaycaster : IVoxelRaycaster
{
    private readonly BlockRegistry _blockRegistry;
    private readonly IChunkManager _chunkManager;

    /// <summary>
    /// Creates a voxel raycaster with the given dependencies.
    /// </summary>
    /// <param name="blockRegistry">Block registry for transparency checks.</param>
    /// <param name="chunkManager">Chunk manager for block lookups across chunks.</param>
    public VoxelRaycaster(BlockRegistry blockRegistry, IChunkManager chunkManager)
    {
        _blockRegistry = blockRegistry;
        _chunkManager = chunkManager;
    }

    /// <summary>
    /// Casts a ray through the voxel world and returns the first non-transparent block hit.
    /// </summary>
    /// <param name="originX">Ray origin X.</param>
    /// <param name="originY">Ray origin Y.</param>
    /// <param name="originZ">Ray origin Z.</param>
    /// <param name="directionX">Ray direction X.</param>
    /// <param name="directionY">Ray direction Y.</param>
    /// <param name="directionZ">Ray direction Z.</param>
    /// <param name="maxDistance">Maximum ray travel distance.</param>
    /// <returns>The raycast result.</returns>
    public VoxelRaycastResult Cast(
        float originX, float originY, float originZ,
        float directionX, float directionY, float directionZ,
        float maxDistance)
    {
        int x = (int)MathF.Floor(originX);
        int y = (int)MathF.Floor(originY);
        int z = (int)MathF.Floor(originZ);

        int stepX = directionX > 0 ? 1 : -1;
        int stepY = directionY > 0 ? 1 : -1;
        int stepZ = directionZ > 0 ? 1 : -1;

        float tMaxX = directionX != 0
            ? (directionX > 0 ? x + 1 - originX : originX - x) / MathF.Abs(directionX)
            : float.MaxValue;
        float tMaxY = directionY != 0
            ? (directionY > 0 ? y + 1 - originY : originY - y) / MathF.Abs(directionY)
            : float.MaxValue;
        float tMaxZ = directionZ != 0
            ? (directionZ > 0 ? z + 1 - originZ : originZ - z) / MathF.Abs(directionZ)
            : float.MaxValue;

        float tDeltaX = directionX != 0 ? 1f / MathF.Abs(directionX) : float.MaxValue;
        float tDeltaY = directionY != 0 ? 1f / MathF.Abs(directionY) : float.MaxValue;
        float tDeltaZ = directionZ != 0 ? 1f / MathF.Abs(directionZ) : float.MaxValue;

        int previousX = x;
        int previousY = y;
        int previousZ = z;
        float distance = 0f;

        while (distance < maxDistance)
        {
            ushort blockId = SampleBlock(x, y, z);

            if (blockId != 0)
            {
                BlockDefinition definition = _blockRegistry.Get(blockId);

                if (!definition.IsTransparent)
                {
                    return new VoxelRaycastResult(
                        Hit: true,
                        HitPosition: new WorldPosition(x, y, z),
                        AdjacentPosition: new WorldPosition(previousX, previousY, previousZ),
                        BlockId: blockId,
                        Distance: distance);
                }
            }

            previousX = x;
            previousY = y;
            previousZ = z;

            if (tMaxX < tMaxY && tMaxX < tMaxZ)
            {
                x += stepX;
                distance = tMaxX;
                tMaxX += tDeltaX;
            }
            else if (tMaxY < tMaxZ)
            {
                y += stepY;
                distance = tMaxY;
                tMaxY += tDeltaY;
            }
            else
            {
                z += stepZ;
                distance = tMaxZ;
                tMaxZ += tDeltaZ;
            }
        }

        return new VoxelRaycastResult(
            Hit: false,
            HitPosition: default,
            AdjacentPosition: default,
            BlockId: 0,
            Distance: maxDistance);
    }

    private ushort SampleBlock(int worldX, int worldY, int worldZ)
    {
        if (worldY is < 0 or >= ChunkData.SizeY)
        {
            return 0;
        }

        ChunkCoord2D chunkCoord = VoxelMath.WorldToChunk(worldX, worldZ, ChunkData.SizeX, ChunkData.SizeZ);
        ChunkCoord coord = new(chunkCoord.ChunkX, chunkCoord.ChunkZ);

        if (!_chunkManager.TryGet(coord, out ChunkEntry? entry) || entry is null)
        {
            return 0;
        }

        if (entry.State < ChunkState.Generated)
        {
            return 0;
        }

        LocalCoord2D local = VoxelMath.WorldToLocal(worldX, worldZ, ChunkData.SizeX, ChunkData.SizeZ);
        return entry.Data.GetBlock(local.LocalX, worldY, local.LocalZ);
    }
}
