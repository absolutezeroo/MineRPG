using System;

using MineRPG.World.Chunks;

namespace MineRPG.World.Generation;

/// <summary>
/// Carves river channels where PeaksAndValleys is near its minimum (valleys).
/// Rivers form natural channels connecting lakes and flowing toward oceans.
/// Thread-safe: stateless, all inputs are provided per call.
/// </summary>
public sealed class RiverCarver
{
    private const int SeaLevel = 62;
    private const float RiverThreshold = -0.6f;
    private const float RiverMaxThreshold = -0.3f;
    private const int RiverMaxDepth = 4;
    private const int RiverMinY = 4;

    private readonly ushort _waterBlockId;
    private readonly ushort _riverBedBlockId;

    /// <summary>
    /// Creates a river carver with the given block IDs.
    /// </summary>
    /// <param name="waterBlockId">Block ID for water.</param>
    /// <param name="riverBedBlockId">Block ID for river bed (e.g., gravel).</param>
    public RiverCarver(ushort waterBlockId, ushort riverBedBlockId)
    {
        _waterBlockId = waterBlockId;
        _riverBedBlockId = riverBedBlockId;
    }

    /// <summary>
    /// Carves a river channel at the given column if PeaksAndValleys is low enough.
    /// Modifies the chunk data in-place.
    /// </summary>
    /// <param name="data">The chunk data to modify.</param>
    /// <param name="localX">Local X coordinate within the chunk.</param>
    /// <param name="localZ">Local Z coordinate within the chunk.</param>
    /// <param name="surfaceY">The surface height at this column.</param>
    /// <param name="peaksAndValleys">PV value at this column (low = valley).</param>
    public void CarveColumn(
        ChunkData data,
        int localX,
        int localZ,
        int surfaceY,
        float peaksAndValleys)
    {
        // Only carve where PV indicates a valley
        if (peaksAndValleys > RiverMaxThreshold)
        {
            return;
        }

        // Don't carve rivers below sea level (they'd be underwater anyway)
        if (surfaceY <= SeaLevel)
        {
            return;
        }

        // Deeper rivers in deeper valleys
        float riverStrength = Math.Clamp(
            (RiverMaxThreshold - peaksAndValleys) / (RiverMaxThreshold - RiverThreshold), 0f, 1f);
        int riverDepth = (int)MathF.Ceiling(riverStrength * RiverMaxDepth);

        if (riverDepth <= 0)
        {
            return;
        }

        int riverBottom = Math.Max(surfaceY - riverDepth, RiverMinY);

        // Set the river bed block at the bottom
        if (_riverBedBlockId != 0)
        {
            data.SetBlock(localX, riverBottom, localZ, _riverBedBlockId);
        }

        // Fill the river channel with water
        for (int y = riverBottom + 1; y <= surfaceY; y++)
        {
            data.SetBlock(localX, y, localZ, _waterBlockId);
        }
    }
}
