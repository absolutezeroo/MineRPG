using System;
using System.Runtime.CompilerServices;

using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Shared coordinate resolution and axis utilities for the meshing pipeline.
/// All methods are static and inlined for hot-path performance.
/// </summary>
internal static class MeshCoordHelper
{
    private const int AxisX = 0;
    private const int AxisY = 1;
    private const int AxisZ = 2;

    /// <summary>
    /// Resolves axis-relative coordinates to world-local (x, y, z).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ResolveCoord(int depthAxis, int uAxis, int vAxis,
        int depthValue, int uValue, int vValue,
        out int x, out int y, out int z)
    {
        x = 0;
        y = 0;
        z = 0;
        SetAxis(depthAxis, depthValue, ref x, ref y, ref z);
        SetAxis(uAxis, uValue, ref x, ref y, ref z);
        SetAxis(vAxis, vValue, ref x, ref y, ref z);
    }

    /// <summary>
    /// Sets the value for the given axis index.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetAxis(int axis, int value, ref int x, ref int y, ref int z)
    {
        switch (axis)
        {
            case AxisX:
                x = value;
                break;
            case AxisY:
                y = value;
                break;
            default:
                z = value;
                break;
        }
    }

    /// <summary>
    /// Gets the dimension size for the given axis.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetDimension(int axis) => axis switch
    {
        AxisX => ChunkData.SizeX,
        AxisY => ChunkData.SizeY,
        _ => ChunkData.SizeZ,
    };

    /// <summary>
    /// Gets the axes for a face direction.
    /// </summary>
    public static void GetAxes(int faceDirection, out int depthAxis, out int uAxis, out int vAxis)
    {
        (depthAxis, uAxis, vAxis) = faceDirection switch
        {
            0 => (AxisX, AxisZ, AxisY), // +X: d=X, u=Z, v=Y
            1 => (AxisX, AxisZ, AxisY), // -X
            2 => (AxisY, AxisX, AxisZ), // +Y: d=Y, u=X, v=Z
            3 => (AxisY, AxisX, AxisZ), // -Y
            4 => (AxisZ, AxisX, AxisY), // +Z: d=Z, u=X, v=Y
            5 => (AxisZ, AxisX, AxisY), // -Z
            _ => throw new ArgumentOutOfRangeException(nameof(faceDirection)),
        };
    }

    /// <summary>
    /// Gets the normal vector for a face direction.
    /// </summary>
    public static (int NormalX, int NormalY, int NormalZ) GetNormal(int faceDirection) => faceDirection switch
    {
        0 => (1, 0, 0),
        1 => (-1, 0, 0),
        2 => (0, 1, 0),
        3 => (0, -1, 0),
        4 => (0, 0, 1),
        5 => (0, 0, -1),
        _ => throw new ArgumentOutOfRangeException(nameof(faceDirection)),
    };
}
