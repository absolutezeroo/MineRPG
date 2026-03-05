using System;
using System.Runtime.CompilerServices;
using System.Threading;

using MineRPG.Core.Math;

namespace MineRPG.World.Chunks;

/// <summary>
/// Stores block IDs for one 16x256x16 chunk as a cache-friendly flat ushort array.
/// Index formula: x + z*SizeX + y*SizeX*SizeZ (matches VoxelMath.GetIndex).
/// </summary>
public sealed class ChunkData
{
    /// <summary>Chunk width in blocks (X axis).</summary>
    public const int SizeX = 16;

    /// <summary>Chunk height in blocks (Y axis).</summary>
    public const int SizeY = 256;

    /// <summary>Chunk depth in blocks (Z axis).</summary>
    public const int SizeZ = 16;

    /// <summary>Total number of blocks in one chunk.</summary>
    public const int TotalBlocks = SizeX * SizeY * SizeZ;

    private readonly ushort[] _blocks = new ushort[TotalBlocks];
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

    /// <summary>The chunk coordinate in the world grid.</summary>
    public ChunkCoord Coord { get; }

    /// <summary>
    /// Creates a new chunk data instance for the given coordinate.
    /// </summary>
    /// <param name="coord">The chunk coordinate.</param>
    public ChunkData(ChunkCoord coord)
    {
        Coord = coord;
    }

    /// <summary>
    /// Computes the flat array index for a local block position.
    /// </summary>
    /// <param name="x">Local X coordinate.</param>
    /// <param name="y">Local Y coordinate.</param>
    /// <param name="z">Local Z coordinate.</param>
    /// <returns>The flat array index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndex(int x, int y, int z)
        => VoxelMath.GetIndex(x, y, z, SizeX, SizeZ);

    /// <summary>
    /// Gets the block ID at the specified local position.
    /// </summary>
    /// <param name="x">Local X coordinate.</param>
    /// <param name="y">Local Y coordinate.</param>
    /// <param name="z">Local Z coordinate.</param>
    /// <returns>The block ID at the given position.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetBlock(int x, int y, int z) => _blocks[GetIndex(x, y, z)];

    /// <summary>
    /// Sets the block ID at the specified local position.
    /// </summary>
    /// <param name="x">Local X coordinate.</param>
    /// <param name="y">Local Y coordinate.</param>
    /// <param name="z">Local Z coordinate.</param>
    /// <param name="blockId">The block ID to set.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetBlock(int x, int y, int z, ushort blockId)
        => _blocks[GetIndex(x, y, z)] = blockId;

    /// <summary>
    /// Returns a read-only span over the raw block array.
    /// </summary>
    /// <returns>A span of all block IDs.</returns>
    public ReadOnlySpan<ushort> GetRawSpan() => _blocks.AsSpan();

    /// <summary>
    /// Overwrites the entire block array from a span. Used for deserialization.
    /// The span must contain exactly <see cref="TotalBlocks"/> elements.
    /// </summary>
    /// <param name="source">The source span to copy from.</param>
    public void LoadFromSpan(ReadOnlySpan<ushort> source)
    {
        if (source.Length != TotalBlocks)
        {
            throw new ArgumentException(
                $"Source span length ({source.Length}) does not match TotalBlocks ({TotalBlocks}).");
        }

        source.CopyTo(_blocks);
    }

    /// <summary>
    /// Checks whether the given local coordinates are within chunk bounds.
    /// </summary>
    /// <param name="x">Local X coordinate.</param>
    /// <param name="y">Local Y coordinate.</param>
    /// <param name="z">Local Z coordinate.</param>
    /// <returns>True if the coordinates are in bounds.</returns>
    public static bool IsInBounds(int x, int y, int z)
        => (uint)x < SizeX && (uint)y < SizeY && (uint)z < SizeZ;

    /// <summary>
    /// Computes metadata for each 16x16x16 sub-chunk. Call after generation completes.
    /// Returns an array of <see cref="SubChunkConstants.SubChunkCount"/> entries.
    /// </summary>
    /// <returns>An array of sub-chunk metadata.</returns>
    public SubChunkInfo[] ComputeSubChunkInfo()
    {
        SubChunkInfo[] result = new SubChunkInfo[SubChunkConstants.SubChunkCount];

        for (int subChunkY = 0; subChunkY < SubChunkConstants.SubChunkCount; subChunkY++)
        {
            int minY = subChunkY * SubChunkConstants.SubChunkSize;
            int nonAirCount = 0;

            for (int y = minY; y < minY + SubChunkConstants.SubChunkSize; y++)
            {
                for (int z = 0; z < SizeZ; z++)
                {
                    for (int x = 0; x < SizeX; x++)
                    {
                        if (_blocks[GetIndex(x, y, z)] != 0)
                        {
                            nonAirCount++;
                        }
                    }
                }
            }

            bool isEmpty = nonAirCount == 0;
            bool isFullySolid = nonAirCount == SubChunkConstants.BlocksPerSubChunk;
            result[subChunkY] = new SubChunkInfo(subChunkY, isEmpty, isFullySolid, nonAirCount);
        }

        return result;
    }

    /// <summary>
    /// Returns the highest Y coordinate that contains a non-air block, or -1 if the chunk is empty.
    /// Useful for occlusion culling and LOD decisions.
    /// </summary>
    /// <returns>The highest non-air Y coordinate, or -1.</returns>
    public int GetHighestNonAirY()
    {
        for (int y = SizeY - 1; y >= 0; y--)
        {
            for (int z = 0; z < SizeZ; z++)
            {
                for (int x = 0; x < SizeX; x++)
                {
                    if (_blocks[GetIndex(x, y, z)] != 0)
                    {
                        return y;
                    }
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Acquires a read lock for cross-thread access to block data.
    /// Must be paired with <see cref="ReleaseReadLock"/>.
    /// Multiple readers can hold the lock concurrently; writers are excluded.
    /// </summary>
    public void AcquireReadLock() => _lock.EnterReadLock();

    /// <summary>
    /// Releases the read lock acquired by <see cref="AcquireReadLock"/>.
    /// </summary>
    public void ReleaseReadLock() => _lock.ExitReadLock();

    /// <summary>
    /// Acquires the write lock for cross-thread block modifications.
    /// Must be paired with <see cref="ReleaseWriteLock"/>.
    /// Excludes both readers and other writers.
    /// </summary>
    public void AcquireWriteLock() => _lock.EnterWriteLock();

    /// <summary>
    /// Releases the write lock acquired by <see cref="AcquireWriteLock"/>.
    /// </summary>
    public void ReleaseWriteLock() => _lock.ExitWriteLock();

    /// <summary>
    /// Copies the raw block data into a caller-provided buffer while holding a read lock.
    /// Use this in background threads to obtain a consistent snapshot for meshing.
    /// The buffer must be at least <see cref="TotalBlocks"/> elements long.
    /// </summary>
    /// <param name="destination">Destination span to copy block data into.</param>
    public void CopyBlocksUnderReadLock(Span<ushort> destination)
    {
        _lock.EnterReadLock();

        try
        {
            _blocks.AsSpan().CopyTo(destination);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}
