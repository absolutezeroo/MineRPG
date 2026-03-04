using System;

using MineRPG.Core.Math;
using MineRPG.World.Chunks;

namespace MineRPG.World.Generation;

/// <summary>
/// Enhanced river carver with meander noise, variable width, depth modulation,
/// and temperature-based freezing. Carves natural-looking river channels.
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class EnhancedRiverCarver
{
    private const int SeaLevel = 62;
    private const float RiverThreshold = -0.6f;
    private const float RiverMaxThreshold = -0.3f;
    private const int RiverMaxDepth = 6;
    private const int RiverMinY = 4;
    private const float MeanderFrequency = 0.008f;
    private const float MeanderAmplitude = 3.0f;
    private const float WidthNoiseFrequency = 0.012f;
    private const float FreezeTemperatureThreshold = -0.5f;
    private const int MinRiverWidth = 2;
    private const int MaxRiverWidth = 5;

    private readonly ushort _waterBlockId;
    private readonly ushort _riverBedBlockId;
    private readonly ushort _iceBlockId;
    private readonly FastNoise _meanderNoise;
    private readonly FastNoise _widthNoise;

    /// <summary>
    /// Creates an enhanced river carver with meander and width modulation.
    /// </summary>
    /// <param name="waterBlockId">Block ID for water.</param>
    /// <param name="riverBedBlockId">Block ID for river bed (e.g., gravel).</param>
    /// <param name="iceBlockId">Block ID for ice (frozen rivers). Use 0 to disable.</param>
    /// <param name="worldSeed">World seed for noise generation.</param>
    public EnhancedRiverCarver(
        ushort waterBlockId,
        ushort riverBedBlockId,
        ushort iceBlockId,
        int worldSeed)
    {
        _waterBlockId = waterBlockId;
        _riverBedBlockId = riverBedBlockId;
        _iceBlockId = iceBlockId;

        _meanderNoise = new FastNoise(unchecked(worldSeed ^ (int)0xAABBCCDD));
        _widthNoise = new FastNoise(unchecked(worldSeed ^ (int)0xDDCCBBAA));
    }

    /// <summary>
    /// Carves a river channel at the given column if PeaksAndValleys is low enough.
    /// Uses meander noise for natural curves and width modulation.
    /// </summary>
    /// <param name="data">The chunk data to modify.</param>
    /// <param name="localX">Local X coordinate within the chunk.</param>
    /// <param name="localZ">Local Z coordinate within the chunk.</param>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <param name="surfaceY">The surface height at this column.</param>
    /// <param name="peaksAndValleys">PV value at this column.</param>
    /// <param name="temperature">Temperature at this column for freeze check.</param>
    public void CarveColumn(
        ChunkData data,
        int localX,
        int localZ,
        int worldX,
        int worldZ,
        int surfaceY,
        float peaksAndValleys,
        float temperature)
    {
        if (peaksAndValleys > RiverMaxThreshold)
        {
            return;
        }

        if (surfaceY <= SeaLevel)
        {
            return;
        }

        // Apply meander offset to the PV threshold check
        float meander = _meanderNoise.Sample2D(
            worldX * MeanderFrequency,
            worldZ * MeanderFrequency) * MeanderAmplitude;

        float adjustedPV = peaksAndValleys + meander * 0.05f;

        if (adjustedPV > RiverMaxThreshold)
        {
            return;
        }

        // Compute river strength and depth
        float riverStrength = Math.Clamp(
            (RiverMaxThreshold - adjustedPV) / (RiverMaxThreshold - RiverThreshold), 0f, 1f);

        int riverDepth = (int)MathF.Ceiling(riverStrength * RiverMaxDepth);

        if (riverDepth <= 0)
        {
            return;
        }

        // Compute variable width from noise
        float widthNoise = _widthNoise.Sample2D(
            worldX * WidthNoiseFrequency,
            worldZ * WidthNoiseFrequency);
        float widthFraction = (widthNoise + 1f) * 0.5f;
        int riverWidth = MinRiverWidth + (int)(widthFraction * (MaxRiverWidth - MinRiverWidth));

        // Only carve the center column (width expansion is handled by neighbor checks)
        int riverBottom = Math.Max(surfaceY - riverDepth, RiverMinY);
        bool isFrozen = temperature < FreezeTemperatureThreshold && _iceBlockId != 0;

        // Set river bed
        if (_riverBedBlockId != 0)
        {
            data.SetBlock(localX, riverBottom, localZ, _riverBedBlockId);
        }

        // Fill river channel with water
        for (int y = riverBottom + 1; y <= surfaceY; y++)
        {
            if (y == surfaceY && isFrozen)
            {
                data.SetBlock(localX, y, localZ, _iceBlockId);
            }
            else
            {
                data.SetBlock(localX, y, localZ, _waterBlockId);
            }
        }
    }

    /// <summary>
    /// Gets the effective river width at a world position for neighbor expansion.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>River half-width in blocks.</returns>
    public int GetRiverHalfWidth(int worldX, int worldZ)
    {
        float widthNoise = _widthNoise.Sample2D(
            worldX * WidthNoiseFrequency,
            worldZ * WidthNoiseFrequency);
        float widthFraction = (widthNoise + 1f) * 0.5f;
        int riverWidth = MinRiverWidth + (int)(widthFraction * (MaxRiverWidth - MinRiverWidth));
        return riverWidth / 2;
    }
}
