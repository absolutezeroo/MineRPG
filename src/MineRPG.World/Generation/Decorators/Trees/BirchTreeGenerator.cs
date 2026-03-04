using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.Decorators.Trees;

/// <summary>
/// Generates a birch tree with a straight tall trunk and narrow canopy.
/// </summary>
public sealed class BirchTreeGenerator : ITreeGenerator
{
    private const int MinTrunkHeight = 5;
    private const int MaxTrunkHeight = 8;
    private const int CanopyRadius = 2;

    private readonly ushort _trunkBlockId;
    private readonly ushort _leavesBlockId;

    /// <summary>
    /// Creates a birch tree generator.
    /// </summary>
    /// <param name="trunkBlockId">Block ID for birch wood.</param>
    /// <param name="leavesBlockId">Block ID for birch leaves.</param>
    public BirchTreeGenerator(ushort trunkBlockId, ushort leavesBlockId)
    {
        _trunkBlockId = trunkBlockId;
        _leavesBlockId = leavesBlockId;
    }

    /// <inheritdoc />
    public string TypeId => "birch_tree";

    /// <inheritdoc />
    public void Generate(ChunkData data, int localX, int baseY, int localZ, Random random)
    {
        int trunkHeight = random.Next(MinTrunkHeight, MaxTrunkHeight + 1);

        // Place trunk
        for (int y = baseY; y < baseY + trunkHeight; y++)
        {
            if (ChunkData.IsInBounds(localX, y, localZ))
            {
                data.SetBlock(localX, y, localZ, _trunkBlockId);
            }
        }

        // Canopy: narrower than oak, starts higher
        int canopyBottom = baseY + trunkHeight - 2;
        int canopyTop = baseY + trunkHeight + 1;

        for (int y = canopyBottom; y <= canopyTop; y++)
        {
            int radius = y == canopyTop ? 1 : CanopyRadius;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    if (Math.Abs(dx) == radius && Math.Abs(dz) == radius)
                    {
                        continue;
                    }

                    int leafX = localX + dx;
                    int leafZ = localZ + dz;

                    if (ChunkData.IsInBounds(leafX, y, leafZ) && data.GetBlock(leafX, y, leafZ) == 0)
                    {
                        data.SetBlock(leafX, y, leafZ, _leavesBlockId);
                    }
                }
            }
        }
    }
}
