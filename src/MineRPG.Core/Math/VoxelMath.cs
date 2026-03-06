using System;
using System.Runtime.CompilerServices;

namespace MineRPG.Core.Math;

/// <summary>
/// Utility math for voxel index conversions and direction tables.
/// All methods are static and inlined for use in hot meshing/lighting paths.
///
/// Coordinate conventions:
///   X = East (+)  / West (-)
///   Y = Up (+)    / Down (-)
///   Z = South (+) / North (-)
///
/// Chunk flat array index formula: x + z * SizeX + y * SizeX * SizeZ
/// </summary>
public static class VoxelMath
{
    private const float SmoothStepCoefficient = 3f;
    private const float SmoothStepSubtractor = 2f;

    /// <summary>
    /// The 6 cardinal face directions as <see cref="Direction3D"/> values.
    /// Index matches FaceDirection enum: Right=0, Left=1, Up=2, Down=3, Front=4, Back=5.
    /// </summary>
    public static readonly Direction3D[] FaceDirections =
    [
        new(1, 0, 0),    // Right  (+X)
        new(-1, 0, 0),   // Left   (-X)
        new(0, 1, 0),    // Up     (+Y)
        new(0, -1, 0),   // Down   (-Y)
        new(0, 0, 1),    // Front  (+Z)
        new(0, 0, -1),   // Back   (-Z)
    ];

    /// <summary>
    /// Converts 3D voxel coordinates to a flat array index.
    /// </summary>
    /// <param name="x">Local X coordinate within the chunk.</param>
    /// <param name="y">Local Y coordinate within the chunk.</param>
    /// <param name="z">Local Z coordinate within the chunk.</param>
    /// <param name="sizeX">Chunk size along the X axis.</param>
    /// <param name="sizeZ">Chunk size along the Z axis.</param>
    /// <returns>The flat array index.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndex(int x, int y, int z, int sizeX, int sizeZ)
        => x + z * sizeX + y * sizeX * sizeZ;

    /// <summary>
    /// Converts a flat array index back to 3D voxel coordinates.
    /// </summary>
    /// <param name="index">The flat array index.</param>
    /// <param name="sizeX">Chunk size along the X axis.</param>
    /// <param name="sizeZ">Chunk size along the Z axis.</param>
    /// <returns>The 3D coordinates as a <see cref="VoxelPosition3D"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static VoxelPosition3D GetPosition(int index, int sizeX, int sizeZ)
    {
        int sliceArea = sizeX * sizeZ;
        int y = index / sliceArea;
        int remainder = index % sliceArea;
        int z = remainder / sizeX;
        int x = remainder % sizeX;
        return new VoxelPosition3D(x, y, z);
    }

    /// <summary>
    /// Convert a world-space position to its containing chunk coordinate.
    /// Handles negative positions correctly with floor division.
    /// </summary>
    /// <param name="worldX">World-space X coordinate.</param>
    /// <param name="worldZ">World-space Z coordinate.</param>
    /// <param name="chunkSizeX">Chunk size along the X axis.</param>
    /// <param name="chunkSizeZ">Chunk size along the Z axis.</param>
    /// <returns>The chunk coordinates as a <see cref="ChunkCoord2D"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ChunkCoord2D WorldToChunk(int worldX, int worldZ, int chunkSizeX, int chunkSizeZ)
    {
        int chunkX = worldX >= 0 ? worldX / chunkSizeX : (worldX - chunkSizeX + 1) / chunkSizeX;
        int chunkZ = worldZ >= 0 ? worldZ / chunkSizeZ : (worldZ - chunkSizeZ + 1) / chunkSizeZ;
        return new ChunkCoord2D(chunkX, chunkZ);
    }

    /// <summary>
    /// Convert a world-space position to its local position within a chunk.
    /// Result is always in [0, chunkSize).
    /// </summary>
    /// <param name="worldX">World-space X coordinate.</param>
    /// <param name="worldZ">World-space Z coordinate.</param>
    /// <param name="chunkSizeX">Chunk size along the X axis.</param>
    /// <param name="chunkSizeZ">Chunk size along the Z axis.</param>
    /// <returns>The local coordinates as a <see cref="LocalCoord2D"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static LocalCoord2D WorldToLocal(int worldX, int worldZ, int chunkSizeX, int chunkSizeZ)
    {
        int localX = ((worldX % chunkSizeX) + chunkSizeX) % chunkSizeX;
        int localZ = ((worldZ % chunkSizeZ) + chunkSizeZ) % chunkSizeZ;
        return new LocalCoord2D(localX, localZ);
    }

    /// <summary>
    /// Linearly interpolates between two values.
    /// </summary>
    /// <param name="a">Start value.</param>
    /// <param name="b">End value.</param>
    /// <param name="t">Interpolation factor in [0, 1].</param>
    /// <returns>The interpolated value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    /// <summary>
    /// Clamps a float value between min and max.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum bound.</param>
    /// <param name="max">The maximum bound.</param>
    /// <returns>The clamped value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(float value, float min, float max)
        => System.Math.Clamp(value, min, max);

    /// <summary>
    /// Clamps an integer value between min and max.
    /// </summary>
    /// <param name="value">The value to clamp.</param>
    /// <param name="min">The minimum bound.</param>
    /// <param name="max">The maximum bound.</param>
    /// <returns>The clamped value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(int value, int min, int max)
        => System.Math.Clamp(value, min, max);

    /// <summary>
    /// Hermite smooth step interpolation between two edges.
    /// </summary>
    /// <param name="edge0">Lower edge of the transition.</param>
    /// <param name="edge1">Upper edge of the transition.</param>
    /// <param name="x">The input value.</param>
    /// <returns>Smoothly interpolated value in [0, 1].</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SmoothStep(float edge0, float edge1, float x)
    {
        if (edge1 <= edge0)
        {
            return x >= edge0 ? 1f : 0f;
        }

        float t = Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (SmoothStepCoefficient - SmoothStepSubtractor * t);
    }
}
