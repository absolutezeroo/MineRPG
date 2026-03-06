using System.Runtime.CompilerServices;

using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Samples block IDs from a chunk or its cardinal neighbors.
/// Handles out-of-bounds coordinates by resolving to the correct neighbor.
/// </summary>
internal static class BlockSampler
{
    /// <summary>
    /// Samples a block ID at the given local coordinates, falling through
    /// to neighbor chunks when out of bounds.
    /// </summary>
    /// <param name="main">The primary chunk.</param>
    /// <param name="neighbors">Cardinal neighbors: [0]=+X, [1]=-X, [2]=+Z, [3]=-Z.</param>
    /// <param name="worldX">Local X coordinate (may be out of bounds).</param>
    /// <param name="worldY">Local Y coordinate.</param>
    /// <param name="worldZ">Local Z coordinate (may be out of bounds).</param>
    /// <returns>The block ID, or 0 if out of world bounds or neighbor is null.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ushort SampleBlock(ChunkData main, ChunkData?[] neighbors, int worldX, int worldY, int worldZ)
    {
        if (worldY is < 0 or >= ChunkData.SizeY)
        {
            return 0;
        }

        if (ChunkData.IsInBounds(worldX, worldY, worldZ))
        {
            return main.GetBlock(worldX, worldY, worldZ);
        }

        int neighborDirectionX = 0, neighborDirectionZ = 0;
        int localX = worldX, localZ = worldZ;

        if (worldX < 0)
        {
            neighborDirectionX = -1;
            localX = worldX + ChunkData.SizeX;
        }
        else if (worldX >= ChunkData.SizeX)
        {
            neighborDirectionX = 1;
            localX = worldX - ChunkData.SizeX;
        }

        if (worldZ < 0)
        {
            neighborDirectionZ = -1;
            localZ = worldZ + ChunkData.SizeZ;
        }
        else if (worldZ >= ChunkData.SizeZ)
        {
            neighborDirectionZ = 1;
            localZ = worldZ - ChunkData.SizeZ;
        }

        // Diagonal case: both X and Z are out of bounds — no diagonal neighbor available
        if (neighborDirectionX != 0 && neighborDirectionZ != 0)
        {
            return 0;
        }

        ChunkData? neighbor = null;

        if (neighborDirectionX == 1)
        {
            neighbor = neighbors[0];
        }
        else if (neighborDirectionX == -1)
        {
            neighbor = neighbors[1];
        }
        else if (neighborDirectionZ == 1)
        {
            neighbor = neighbors[2];
        }
        else if (neighborDirectionZ == -1)
        {
            neighbor = neighbors[3];
        }

        return neighbor?.GetBlock(localX, worldY, localZ) ?? (ushort)0;
    }
}
