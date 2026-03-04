using System;
using System.Runtime.CompilerServices;

using MineRPG.Core.Math;

namespace MineRPG.World.Generation.Aquifers;

/// <summary>
/// Determines whether underground cavities are flooded with water, lava, or air.
/// Each aquifer has an independent water level determined by noise on a coarse grid.
/// Barriers are generated between adjacent aquifers with different levels.
/// Thread-safe: all state is readonly after construction.
/// </summary>
public sealed class AquiferSampler : IAquiferSampler
{
    private const int SeedFloodedness = 0x11223344;
    private const int SeedLevel = 0x55667788;
    private const int SeedBarrier = 0x99AABB00;

    private readonly FastNoise _floodednessNoise;
    private readonly FastNoise _levelNoise;
    private readonly FastNoise _barrierNoise;
    private readonly AquiferConfig _config;
    private readonly ushort _waterBlockId;
    private readonly ushort _lavaBlockId;

    /// <summary>
    /// Creates an aquifer sampler with the given configuration.
    /// </summary>
    /// <param name="config">Aquifer configuration data.</param>
    /// <param name="worldSeed">World seed for noise generation.</param>
    /// <param name="waterBlockId">Block ID for water.</param>
    /// <param name="lavaBlockId">Block ID for lava.</param>
    public AquiferSampler(AquiferConfig config, int worldSeed, ushort waterBlockId, ushort lavaBlockId)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _waterBlockId = waterBlockId;
        _lavaBlockId = lavaBlockId;

        _floodednessNoise = new FastNoise(worldSeed ^ SeedFloodedness);
        _levelNoise = new FastNoise(worldSeed ^ SeedLevel);
        _barrierNoise = new FastNoise(worldSeed ^ SeedBarrier);
    }

    /// <inheritdoc />
    public ushort GetFluidBlock(int worldX, int worldY, int worldZ, int surfaceY)
    {
        // Near-surface cavities are always dry
        if (worldY > surfaceY - _config.SurfaceClearance)
        {
            return 0;
        }

        FloodednessState state = DetermineFloodedness(worldX, worldY, worldZ);

        if (state == FloodednessState.Empty || state == FloodednessState.Disabled)
        {
            return 0;
        }

        int aquiferLevel = DetermineAquiferLevel(worldX, worldY, worldZ);

        // Check for barrier between adjacent aquifers
        if (IsBarrierPosition(worldX, worldY, worldZ))
        {
            return 0;
        }

        // Only flood if the position is at or below the aquifer's water level
        if (worldY > aquiferLevel)
        {
            return 0;
        }

        // Below lava level, aquifers contain lava
        if (worldY <= _config.LavaLevel)
        {
            return _lavaBlockId;
        }

        return _waterBlockId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private FloodednessState DetermineFloodedness(int worldX, int worldY, int worldZ)
    {
        float floodedness = _floodednessNoise.Sample3D(
            worldX * _config.FloodednessFrequency,
            worldY * _config.FloodednessFrequency,
            worldZ * _config.FloodednessFrequency);

        if (floodedness > _config.FloodednessThreshold)
        {
            return FloodednessState.Flooded;
        }

        if (floodedness < -_config.FloodednessThreshold)
        {
            return FloodednessState.Empty;
        }

        return FloodednessState.Randomized;
    }

    private int DetermineAquiferLevel(int worldX, int worldY, int worldZ)
    {
        // Above the deep threshold, use sea level as default
        if (worldY > _config.DeepAquiferThreshold)
        {
            return _config.SeaLevel;
        }

        // Snap to the coarse grid for aquifer level sampling
        int gridSpacing = _config.GridSpacing;
        int gridX = (int)MathF.Floor((float)worldX / gridSpacing) * gridSpacing;
        int gridY = (int)MathF.Floor((float)worldY / gridSpacing) * gridSpacing;
        int gridZ = (int)MathF.Floor((float)worldZ / gridSpacing) * gridSpacing;

        float levelNoise = _levelNoise.Sample3D(
            gridX * _config.LevelNoiseFrequency,
            gridY * _config.LevelNoiseFrequency,
            gridZ * _config.LevelNoiseFrequency);

        // Map noise [-1, 1] to a water level offset
        int baseLevel = gridY + gridSpacing / 2;
        int levelOffset = (int)(levelNoise * _config.LevelVariation);

        return baseLevel + levelOffset;
    }

    private bool IsBarrierPosition(int worldX, int worldY, int worldZ)
    {
        int gridSpacing = _config.GridSpacing;
        int barrierWidth = _config.BarrierWidth;

        // Check if near a grid boundary on any axis
        int modX = ((worldX % gridSpacing) + gridSpacing) % gridSpacing;
        int modY = ((worldY % gridSpacing) + gridSpacing) % gridSpacing;
        int modZ = ((worldZ % gridSpacing) + gridSpacing) % gridSpacing;

        bool nearBoundaryX = modX < barrierWidth || modX >= gridSpacing - barrierWidth;
        bool nearBoundaryY = modY < barrierWidth || modY >= gridSpacing - barrierWidth;
        bool nearBoundaryZ = modZ < barrierWidth || modZ >= gridSpacing - barrierWidth;

        if (!nearBoundaryX && !nearBoundaryY && !nearBoundaryZ)
        {
            return false;
        }

        // Use barrier noise to make barriers irregular (not straight walls)
        float noise = _barrierNoise.Sample3D(
            worldX * 0.1f, worldY * 0.1f, worldZ * 0.1f);

        return noise > 0.2f;
    }
}
