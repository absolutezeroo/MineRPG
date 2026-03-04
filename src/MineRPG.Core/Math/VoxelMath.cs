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
    // The 6 cardinal face directions as (dx, dy, dz) tuples.
    // Index matches FaceDirection enum: Right=0, Left=1, Up=2, Down=3, Front=4, Back=5
    public static readonly (int Dx, int Dy, int Dz)[] FaceDirections =
    [
        (1, 0, 0),    // Right  (+X)
        (-1, 0, 0),   // Left   (-X)
        (0, 1, 0),    // Up     (+Y)
        (0, -1, 0),   // Down   (-Y)
        (0, 0, 1),    // Front  (+Z)
        (0, 0, -1),   // Back   (-Z)
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetIndex(int x, int y, int z, int sizeX, int sizeZ)
        => x + z * sizeX + y * sizeX * sizeZ;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int X, int Y, int Z) GetPosition(int index, int sizeX, int sizeZ)
    {
        var y = index / (sizeX * sizeZ);
        var remainder = index % (sizeX * sizeZ);
        var z = remainder / sizeX;
        var x = remainder % sizeX;
        return (x, y, z);
    }

    /// <summary>
    /// Convert a world-space position to its containing chunk coordinate.
    /// Handles negative positions correctly with floor division.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int ChunkX, int ChunkZ) WorldToChunk(int worldX, int worldZ, int chunkSizeX, int chunkSizeZ)
    {
        var cx = worldX >= 0 ? worldX / chunkSizeX : (worldX - chunkSizeX + 1) / chunkSizeX;
        var cz = worldZ >= 0 ? worldZ / chunkSizeZ : (worldZ - chunkSizeZ + 1) / chunkSizeZ;
        return (cx, cz);
    }

    /// <summary>
    /// Convert a world-space position to its local position within a chunk.
    /// Result is always in [0, chunkSize).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static (int LocalX, int LocalZ) WorldToLocal(int worldX, int worldZ, int chunkSizeX, int chunkSizeZ)
    {
        var lx = ((worldX % chunkSizeX) + chunkSizeX) % chunkSizeX;
        var lz = ((worldZ % chunkSizeZ) + chunkSizeZ) % chunkSizeZ;
        return (lx, lz);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(float value, float min, float max)
        => value < min ? min : value > max ? max : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Clamp(int value, int min, int max)
        => value < min ? min : value > max ? max : value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SmoothStep(float edge0, float edge1, float x)
    {
        var t = Clamp((x - edge0) / (edge1 - edge0), 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
