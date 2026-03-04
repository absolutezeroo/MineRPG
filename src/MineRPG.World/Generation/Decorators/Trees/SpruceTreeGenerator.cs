using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.Decorators.Trees;

/// <summary>
/// Generates a spruce/fir tree with a conical canopy shape.
/// </summary>
public sealed class SpruceTreeGenerator : ITreeGenerator
{
    private const int MinTrunkHeight = 6;
    private const int MaxTrunkHeight = 10;
    private const int MaxCanopyRadius = 3;

    private readonly ushort _trunkBlockId;
    private readonly ushort _leavesBlockId;

    /// <summary>
    /// Creates a spruce tree generator.
    /// </summary>
    /// <param name="trunkBlockId">Block ID for spruce wood.</param>
    /// <param name="leavesBlockId">Block ID for spruce leaves.</param>
    public SpruceTreeGenerator(ushort trunkBlockId, ushort leavesBlockId)
    {
        _trunkBlockId = trunkBlockId;
        _leavesBlockId = leavesBlockId;
    }

    /// <inheritdoc />
    public string TypeId => "spruce_tree";

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

        // Conical canopy: wider at bottom, narrow at top
        int canopyBottom = baseY + 2;
        int canopyTop = baseY + trunkHeight;

        for (int y = canopyBottom; y <= canopyTop; y++)
        {
            float progress = (float)(y - canopyBottom) / (canopyTop - canopyBottom);
            int radius = (int)(MaxCanopyRadius * (1f - progress));

            if (radius < 0)
            {
                radius = 0;
            }

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dz = -radius; dz <= radius; dz++)
                {
                    // Diamond shape for conifers
                    if (Math.Abs(dx) + Math.Abs(dz) > radius)
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

        // Top spike
        int topY = canopyTop + 1;

        if (ChunkData.IsInBounds(localX, topY, localZ))
        {
            data.SetBlock(localX, topY, localZ, _leavesBlockId);
        }
    }
}
