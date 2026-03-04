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
    BiomeSelector biomeSelector) : IDebugDataProvider
{
    public float PlayerX => playerData.PositionX;
    public float PlayerY => playerData.PositionY;
    public float PlayerZ => playerData.PositionZ;

    public int ChunkX
    {
        get
        {
            var (cx, _) = VoxelMath.WorldToChunk(
                (int)MathF.Floor(playerData.PositionX),
                (int)MathF.Floor(playerData.PositionZ),
                ChunkData.SizeX,
                ChunkData.SizeZ);
            return cx;
        }
    }

    public int ChunkZ
    {
        get
        {
            var (_, cz) = VoxelMath.WorldToChunk(
                (int)MathF.Floor(playerData.PositionX),
                (int)MathF.Floor(playerData.PositionZ),
                ChunkData.SizeX,
                ChunkData.SizeZ);
            return cz;
        }
    }

    public string CurrentBiome
    {
        get
        {
            var biome = biomeSelector.Select(
                (int)MathF.Floor(playerData.PositionX),
                (int)MathF.Floor(playerData.PositionZ));
            return biome.BiomeType.ToString();
        }
    }

    public int LoadedChunkCount => chunkManager.Count;
}
