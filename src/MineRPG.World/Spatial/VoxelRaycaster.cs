using MineRPG.Core.Math;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;

namespace MineRPG.World.Spatial;

/// <summary>
/// DDA (Digital Differential Analysis) voxel raycast.
/// Operates directly on ChunkData — does not use Godot's physics engine.
/// </summary>
public sealed class VoxelRaycaster(BlockRegistry blockRegistry, IChunkManager chunkManager) : IVoxelRaycaster
{
    public VoxelRaycastResult Cast(
        float ox, float oy, float oz,
        float dx, float dy, float dz,
        float maxDistance)
    {
        var x = (int)MathF.Floor(ox);
        var y = (int)MathF.Floor(oy);
        var z = (int)MathF.Floor(oz);

        var stepX = dx > 0 ? 1 : -1;
        var stepY = dy > 0 ? 1 : -1;
        var stepZ = dz > 0 ? 1 : -1;

        var tMaxX = dx != 0 ? (dx > 0 ? x + 1 - ox : ox - x) / MathF.Abs(dx) : float.MaxValue;
        var tMaxY = dy != 0 ? (dy > 0 ? y + 1 - oy : oy - y) / MathF.Abs(dy) : float.MaxValue;
        var tMaxZ = dz != 0 ? (dz > 0 ? z + 1 - oz : oz - z) / MathF.Abs(dz) : float.MaxValue;

        var tDeltaX = dx != 0 ? 1f / MathF.Abs(dx) : float.MaxValue;
        var tDeltaY = dy != 0 ? 1f / MathF.Abs(dy) : float.MaxValue;
        var tDeltaZ = dz != 0 ? 1f / MathF.Abs(dz) : float.MaxValue;

        var prevX = x;
        var prevY = y;
        var prevZ = z;
        var dist = 0f;

        while (dist < maxDistance)
        {
            var blockId = SampleBlock(x, y, z);
            if (blockId != 0)
            {
                var def = blockRegistry.Get(blockId);
                if (!def.IsTransparent)
                {
                    return new VoxelRaycastResult(
                        Hit: true,
                        HitPosition: new WorldPosition(x, y, z),
                        AdjacentPosition: new WorldPosition(prevX, prevY, prevZ),
                        BlockId: blockId,
                        Distance: dist);
                }
            }

            prevX = x;
            prevY = y;
            prevZ = z;

            if (tMaxX < tMaxY && tMaxX < tMaxZ)
            {
                x += stepX;
                dist = tMaxX;
                tMaxX += tDeltaX;
            }
            else if (tMaxY < tMaxZ)
            {
                y += stepY;
                dist = tMaxY;
                tMaxY += tDeltaY;
            }
            else
            {
                z += stepZ;
                dist = tMaxZ;
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

    private ushort SampleBlock(int wx, int wy, int wz)
    {
        if (wy is < 0 or >= ChunkData.SizeY)
            return 0;

        var (cx, cz) = VoxelMath.WorldToChunk(wx, wz, ChunkData.SizeX, ChunkData.SizeZ);
        var coord = new ChunkCoord(cx, cz);

        if (!chunkManager.TryGet(coord, out var entry) || entry is null)
            return 0;

        if (entry.State < ChunkState.Generated)
            return 0;

        var (lx, lz) = VoxelMath.WorldToLocal(wx, wz, ChunkData.SizeX, ChunkData.SizeZ);
        return entry.Data.GetBlock(lx, wy, lz);
    }
}
