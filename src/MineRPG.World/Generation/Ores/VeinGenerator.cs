using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.Ores;

/// <summary>
/// Generates an irregularly shaped ore vein centered at a position.
/// Uses a random walk to produce blob-like shapes rather than cubes.
/// </summary>
public static class VeinGenerator
{
    private const int MaxDirections = 6;

    // Direction offsets: +X, -X, +Y, -Y, +Z, -Z
    private static readonly int[] DirectionX = { 1, -1, 0, 0, 0, 0 };
    private static readonly int[] DirectionY = { 0, 0, 1, -1, 0, 0 };
    private static readonly int[] DirectionZ = { 0, 0, 0, 0, 1, -1 };

    /// <summary>
    /// Places a vein of ore blocks around a center position.
    /// </summary>
    /// <param name="data">Chunk data to modify.</param>
    /// <param name="centerX">Local X of vein center.</param>
    /// <param name="centerY">Y of vein center.</param>
    /// <param name="centerZ">Local Z of vein center.</param>
    /// <param name="blockId">Ore block ID to place.</param>
    /// <param name="veinSize">Maximum number of blocks in the vein.</param>
    /// <param name="stoneBlockId">Block ID that ore can replace (typically stone).</param>
    /// <param name="random">Seeded random for shape variation.</param>
    /// <returns>The number of blocks actually placed.</returns>
    public static int Generate(
        ChunkData data,
        int centerX,
        int centerY,
        int centerZ,
        ushort blockId,
        int veinSize,
        ushort stoneBlockId,
        Random random)
    {
        int placed = 0;
        int currentX = centerX;
        int currentY = centerY;
        int currentZ = centerZ;

        for (int i = 0; i < veinSize; i++)
        {
            if (ChunkData.IsInBounds(currentX, currentY, currentZ))
            {
                ushort existing = data.GetBlock(currentX, currentY, currentZ);

                if (existing == stoneBlockId)
                {
                    data.SetBlock(currentX, currentY, currentZ, blockId);
                    placed++;
                }
            }

            // Random walk to next position
            int direction = random.Next(MaxDirections);
            currentX += DirectionX[direction];
            currentY += DirectionY[direction];
            currentZ += DirectionZ[direction];
        }

        return placed;
    }
}
