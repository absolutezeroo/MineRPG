using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation.Decorators.Trees;

/// <summary>
/// Generates a classic oak tree with a 4-7 block trunk and spherical canopy.
/// </summary>
public sealed class OakTreeGenerator : ITreeGenerator
{
    private const int MinTrunkHeight = 4;
    private const int MaxTrunkHeight = 7;
    private const int CanopyRadius = 2;
    private const int CanopyTopOffset = 1;

    private readonly ushort _trunkBlockId;
    private readonly ushort _leavesBlockId;

    /// <summary>
    /// Creates an oak tree generator.
    /// </summary>
    /// <param name="trunkBlockId">Block ID for wood.</param>
    /// <param name="leavesBlockId">Block ID for leaves.</param>
    public OakTreeGenerator(ushort trunkBlockId, ushort leavesBlockId)
    {
        _trunkBlockId = trunkBlockId;
        _leavesBlockId = leavesBlockId;
    }

    /// <inheritdoc />
    public string TypeId => "oak_tree";

    /// <inheritdoc />
    public void Generate(ChunkData data, int localX, int baseY, int localZ, Random random)
    {
        int trunkHeight = random.Next(MinTrunkHeight, MaxTrunkHeight + 1);
        int canopyStart = baseY + trunkHeight - CanopyRadius;
        int canopyTop = baseY + trunkHeight + CanopyTopOffset;

        // Place trunk
        for (int y = baseY; y < baseY + trunkHeight; y++)
        {
            if (ChunkData.IsInBounds(localX, y, localZ))
            {
                data.SetBlock(localX, y, localZ, _trunkBlockId);
            }
        }

        // Place canopy (spherical)
        for (int dy = canopyStart; dy <= canopyTop; dy++)
        {
            int radiusAtY = CanopyRadius;

            if (dy == canopyTop)
            {
                radiusAtY = 1;
            }

            for (int dx = -radiusAtY; dx <= radiusAtY; dx++)
            {
                for (int dz = -radiusAtY; dz <= radiusAtY; dz++)
                {
                    // Skip corners for rounder shape
                    if (Math.Abs(dx) == radiusAtY && Math.Abs(dz) == radiusAtY)
                    {
                        continue;
                    }

                    int leafX = localX + dx;
                    int leafZ = localZ + dz;

                    if (ChunkData.IsInBounds(leafX, dy, leafZ))
                    {
                        // Don't overwrite trunk
                        if (data.GetBlock(leafX, dy, leafZ) == 0)
                        {
                            data.SetBlock(leafX, dy, leafZ, _leavesBlockId);
                        }
                    }
                }
            }
        }
    }
}
