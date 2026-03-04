using System;

using MineRPG.Core.Diagnostics;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Math;
using MineRPG.Entities.Player;
using MineRPG.World.Chunks;
using MineRPG.World.Generation;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Bridges World and Entities data into the <see cref="IDebugDataProvider"/> contract
/// so that Godot.UI does not need direct references to those projects.
/// Registered in <see cref="CompositionRoot"/> and read by DebugOverlayNode.
/// </summary>
public sealed class DebugDataProvider(
    PlayerData playerData,
    IChunkManager chunkManager,
    BiomeSelector biomeSelector,
    PerformanceMonitor performanceMonitor) : IDebugDataProvider
{
    /// <summary>
    /// Gets the player X position.
    /// </summary>
    public float PlayerX => playerData.PositionX;

    /// <summary>
    /// Gets the player Y position.
    /// </summary>
    public float PlayerY => playerData.PositionY;

    /// <summary>
    /// Gets the player Z position.
    /// </summary>
    public float PlayerZ => playerData.PositionZ;

    /// <summary>
    /// Gets the chunk X coordinate the player is currently in.
    /// </summary>
    public int ChunkX
    {
        get
        {
            ChunkCoord2D coord = VoxelMath.WorldToChunk(
                (int)MathF.Floor(playerData.PositionX),
                (int)MathF.Floor(playerData.PositionZ),
                ChunkData.SizeX,
                ChunkData.SizeZ);
            return coord.ChunkX;
        }
    }

    /// <summary>
    /// Gets the chunk Z coordinate the player is currently in.
    /// </summary>
    public int ChunkZ
    {
        get
        {
            ChunkCoord2D coord = VoxelMath.WorldToChunk(
                (int)MathF.Floor(playerData.PositionX),
                (int)MathF.Floor(playerData.PositionZ),
                ChunkData.SizeX,
                ChunkData.SizeZ);
            return coord.ChunkZ;
        }
    }

    /// <summary>
    /// Gets the name of the biome the player is currently in.
    /// </summary>
    public string CurrentBiome
    {
        get
        {
            BiomeDefinition biome = biomeSelector.Select(
                (int)MathF.Floor(playerData.PositionX),
                (int)MathF.Floor(playerData.PositionZ));
            return biome.BiomeType.ToString();
        }
    }

    /// <summary>
    /// Gets the total number of loaded chunks.
    /// </summary>
    public int LoadedChunkCount => chunkManager.Count;

    /// <summary>
    /// Gets the number of currently visible chunks.
    /// </summary>
    public int VisibleChunkCount => (int)performanceMonitor.VisibleChunks;

    /// <summary>
    /// Gets the number of chunks currently in the loading queue.
    /// </summary>
    public int ChunksInQueue => (int)performanceMonitor.ChunksInQueue;

    /// <summary>
    /// Gets the average mesh build time in milliseconds.
    /// </summary>
    public double AverageMeshTimeMs => performanceMonitor.AverageMeshTimeMs;

    /// <summary>
    /// Gets the total number of rendered vertices.
    /// </summary>
    public long TotalVertices => performanceMonitor.TotalVertices;

    /// <summary>
    /// Gets the current render distance in chunks.
    /// </summary>
    public int RenderDistance => performanceMonitor.RenderDistance;

    /// <summary>
    /// Gets the number of idle objects in the chunk node pool.
    /// </summary>
    public int PoolIdleCount => (int)performanceMonitor.PoolIdleCount;

    /// <summary>
    /// Gets the number of active objects in the chunk node pool.
    /// </summary>
    public int PoolActiveCount => (int)performanceMonitor.PoolActiveCount;
}
