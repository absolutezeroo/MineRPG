using System;

using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Logging;
using MineRPG.Core.Math;
using MineRPG.Entities.Player;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;
using MineRPG.World.Generation;

namespace MineRPG.Godot.Entities;

/// <summary>
/// Reads the environment around the player and writes state to <see cref="PlayerData"/>.
/// Detects whether the player is underwater and samples the biome temperature.
/// </summary>
internal sealed class PlayerEnvironmentSensor
{
    /// <summary>
    /// Eye offset from player position (CharacterBody3D origin is at feet).
    /// </summary>
    private const float EyeHeightOffset = 1.5f;

    private readonly PlayerData _playerData;
    private readonly IChunkManager _chunkManager;
    private readonly BlockRegistry _blockRegistry;
    private readonly TerrainSampler _terrainSampler;

    private PlayerEnvironmentSensor(
        PlayerData playerData,
        IChunkManager chunkManager,
        BlockRegistry blockRegistry,
        TerrainSampler terrainSampler)
    {
        _playerData = playerData;
        _chunkManager = chunkManager;
        _blockRegistry = blockRegistry;
        _terrainSampler = terrainSampler;
    }

    /// <summary>
    /// Attempts to create an environment sensor by resolving dependencies.
    /// Returns null if required services are not yet registered.
    /// </summary>
    /// <param name="playerData">Player data to write environment state to.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <returns>A new sensor, or null if dependencies are missing.</returns>
    public static PlayerEnvironmentSensor? TryCreate(PlayerData playerData, ILogger logger)
    {
        if (!ServiceLocator.Instance.TryGet<IChunkManager>(out IChunkManager? chunkManager)
            || chunkManager is null)
        {
            logger.Warning("PlayerEnvironmentSensor: IChunkManager not available.");
            return null;
        }

        if (!ServiceLocator.Instance.TryGet<BlockRegistry>(out BlockRegistry? blockRegistry)
            || blockRegistry is null)
        {
            logger.Warning("PlayerEnvironmentSensor: BlockRegistry not available.");
            return null;
        }

        if (!ServiceLocator.Instance.TryGet<TerrainSampler>(out TerrainSampler? terrainSampler)
            || terrainSampler is null)
        {
            logger.Warning("PlayerEnvironmentSensor: TerrainSampler not available.");
            return null;
        }

        return new PlayerEnvironmentSensor(playerData, chunkManager, blockRegistry, terrainSampler);
    }

    /// <summary>
    /// Samples environment at the player's position and updates PlayerData.
    /// </summary>
    /// <param name="worldPosition">The player's current world position.</param>
    public void Tick(Vector3 worldPosition)
    {
        UpdateUnderwaterState(worldPosition);
        UpdateBiomeTemperature(worldPosition);
    }

    private void UpdateUnderwaterState(Vector3 worldPosition)
    {
        int headX = (int)MathF.Floor(worldPosition.X);
        int headY = (int)MathF.Floor(worldPosition.Y + EyeHeightOffset);
        int headZ = (int)MathF.Floor(worldPosition.Z);

        ushort blockId = GetBlockAt(headX, headY, headZ);

        if (blockId == 0)
        {
            _playerData.IsUnderwater = false;
            return;
        }

        BlockDefinition definition = _blockRegistry.Get(blockId);
        _playerData.IsUnderwater = (definition.Flags & BlockFlags.Liquid) != 0;
    }

    private void UpdateBiomeTemperature(Vector3 worldPosition)
    {
        int worldX = (int)MathF.Floor(worldPosition.X);
        int worldZ = (int)MathF.Floor(worldPosition.Z);

        TerrainColumn column = _terrainSampler.SampleColumn(worldX, worldZ);
        float biomeTemp = (column.PrimaryBiome.ClimateTarget.Temperature.Min
            + column.PrimaryBiome.ClimateTarget.Temperature.Max) / 2f;

        _playerData.CurrentBiomeTemperature = biomeTemp;
    }

    private ushort GetBlockAt(int worldX, int worldY, int worldZ)
    {
        if (worldY < 0 || worldY >= ChunkData.SizeY)
        {
            return 0;
        }

        ChunkCoord2D chunkCoord2D = VoxelMath.WorldToChunk(
            worldX, worldZ, ChunkData.SizeX, ChunkData.SizeZ);

        ChunkCoord coord = new(chunkCoord2D.ChunkX, chunkCoord2D.ChunkZ);

        if (!_chunkManager.TryGet(coord, out ChunkEntry? entry) || entry?.Data is null)
        {
            return 0;
        }

        LocalCoord2D localCoord = VoxelMath.WorldToLocal(
            worldX, worldZ, ChunkData.SizeX, ChunkData.SizeZ);

        return entry.Data.GetBlock(localCoord.LocalX, worldY, localCoord.LocalZ);
    }
}
