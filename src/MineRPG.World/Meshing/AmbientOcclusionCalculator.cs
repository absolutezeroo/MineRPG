using System.Runtime.CompilerServices;

using MineRPG.World.Blocks;
using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Computes per-vertex ambient occlusion for voxel faces.
/// Uses the standard voxel AO formula: samples 3 neighboring blocks
/// (two edges + one corner) at the air level. If both edges are solid,
/// AO = 0. Otherwise AO = (3 - solidCount) / 3.
///
/// All methods are static and inlined for hot-path performance.
/// </summary>
internal static class AmbientOcclusionCalculator
{
    private const float AoOcclusionFull = 0f;
    private const float AoDivisor = 3f;

    /// <summary>
    /// Computes ambient occlusion for a vertex at position (uVertex, vVertex)
    /// in the face plane.
    /// </summary>
    /// <param name="chunk">The chunk being meshed.</param>
    /// <param name="neighbors">Cardinal neighbor chunks.</param>
    /// <param name="blockRegistry">Block registry for solidity lookups.</param>
    /// <param name="depthAxis">The face depth axis.</param>
    /// <param name="uAxis">The face U axis.</param>
    /// <param name="vAxis">The face V axis.</param>
    /// <param name="airDepth">The depth coordinate of the air block.</param>
    /// <param name="uVertex">U coordinate of the vertex.</param>
    /// <param name="vVertex">V coordinate of the vertex.</param>
    /// <param name="du">Corner offset in U (0 or width).</param>
    /// <param name="dv">Corner offset in V (0 or height).</param>
    /// <returns>AO value from 0.0 (fully occluded) to 1.0 (fully lit).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Compute(
        ChunkData chunk, ChunkData?[] neighbors, BlockRegistry blockRegistry,
        int depthAxis, int uAxis, int vAxis,
        int airDepth, int uVertex, int vVertex,
        int du, int dv)
    {
        int uOther = (du == 0) ? -1 : 0;
        int vOther = (dv == 0) ? -1 : 0;
        int uAir = (du == 0) ? 0 : -1;
        int vAir = (dv == 0) ? 0 : -1;

        bool side1 = IsSolidAt(chunk, neighbors, blockRegistry, depthAxis, uAxis, vAxis,
            airDepth, uVertex + uOther, vVertex + vAir);
        bool side2 = IsSolidAt(chunk, neighbors, blockRegistry, depthAxis, uAxis, vAxis,
            airDepth, uVertex + uAir, vVertex + vOther);

        if (side1 && side2)
        {
            return AoOcclusionFull;
        }

        bool corner = IsSolidAt(chunk, neighbors, blockRegistry, depthAxis, uAxis, vAxis,
            airDepth, uVertex + uOther, vVertex + vOther);
        int count = (side1 ? 1 : 0) + (side2 ? 1 : 0) + (corner ? 1 : 0);
        return (AoDivisor - count) / AoDivisor;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSolidAt(
        ChunkData chunk, ChunkData?[] neighbors, BlockRegistry blockRegistry,
        int depthAxis, int uAxis, int vAxis,
        int depthValue, int uValue, int vValue)
    {
        MeshCoordHelper.ResolveCoord(depthAxis, uAxis, vAxis, depthValue, uValue, vValue,
            out int x, out int y, out int z);

        ushort blockId = BlockSampler.SampleBlock(chunk, neighbors, x, y, z);
        return blockId != 0 && blockRegistry.Get(blockId).IsSolid;
    }
}
