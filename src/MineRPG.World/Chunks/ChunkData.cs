using System.Runtime.CompilerServices;
using MineRPG.Core.Math;

namespace MineRPG.World.Chunks;

/// <summary>
/// Stores block IDs for one 16x256x16 chunk as a cache-friendly flat ushort array.
/// Index formula: x + z*SizeX + y*SizeX*SizeZ (matches VoxelMath.GetIndex).
/// </summary>
public sealed class ChunkData(ChunkCoord coord)
{
    public const int SizeX = 16;
    public const int SizeY = 256;
    public const int SizeZ = 16;
    public const int TotalBlocks = SizeX * SizeY * SizeZ;

    private readonly ushort[] _blocks = new ushort[TotalBlocks];

    public ChunkCoord Coord { get; } = coord;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndex(int x, int y, int z)
        => VoxelMath.GetIndex(x, y, z, SizeX, SizeZ);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetBlock(int x, int y, int z) => _blocks[GetIndex(x, y, z)];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlock(int x, int y, int z, ushort blockId)
        => _blocks[GetIndex(x, y, z)] = blockId;

    public ReadOnlySpan<ushort> GetRawSpan() => _blocks.AsSpan();

    /// <summary>
    /// Overwrites the entire block array from a span. Used for deserialization.
    /// The span must contain exactly <see cref="TotalBlocks"/> elements.
    /// </summary>
    public void LoadFromSpan(ReadOnlySpan<ushort> source)
    {
        if (source.Length != TotalBlocks)
            throw new ArgumentException(
                $"Source span length ({source.Length}) does not match TotalBlocks ({TotalBlocks}).");

        source.CopyTo(_blocks);
    }

    public static bool IsInBounds(int x, int y, int z)
        => (uint)x < SizeX && (uint)y < SizeY && (uint)z < SizeZ;
}
