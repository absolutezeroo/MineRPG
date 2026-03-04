using System.Buffers;
using System.Runtime.CompilerServices;
using MineRPG.World.Blocks;
using MineRPG.World.Chunks;

namespace MineRPG.World.Meshing;

/// <summary>
/// Greedy mesh builder with per-vertex ambient occlusion.
///
/// For each of the 6 face directions, scans the chunk slice by slice.
/// On each slice, builds a 2D mask of visible faces then merges
/// contiguous same-block faces into larger quads.
///
/// Produces separate mesh data for opaque and liquid faces so they
/// can be rendered with different materials (opaque vs translucent).
///
/// UV channels:
///   UV  = tiling coordinates in block units (0..width, 0..height).
///   UV2 = atlas tile origin (u0, v0).
///
/// Vertex color:
///   RGB = block tint from definition.
///   A   = vertex ambient occlusion (0 = fully occluded, 1 = fully lit).
///
/// Thread-safe: all state is local to each Build() call.
/// </summary>
public sealed class ChunkMeshBuilder(BlockRegistry blockRegistry) : IChunkMeshBuilder
{
    private const int ChunkSizeX = ChunkData.SizeX;
    private const int ChunkSizeY = ChunkData.SizeY;
    private const int ChunkSizeZ = ChunkData.SizeZ;

    public ChunkMeshResult Build(ChunkData chunk, ChunkData?[] neighbors)
    {
        var opaque = new MeshAccumulator(4096);
        var liquid = new MeshAccumulator(512);

        for (var faceDir = 0; faceDir < 6; faceDir++)
        {
            BuildFaceDirection(faceDir, chunk, neighbors, opaque, liquid);
        }

        return new ChunkMeshResult(opaque.ToMeshData(), liquid.ToMeshData());
    }

    private void BuildFaceDirection(
        int faceDir,
        ChunkData chunk,
        ChunkData?[] neighbors,
        MeshAccumulator opaque,
        MeshAccumulator liquid)
    {
        GetAxes(faceDir, out var d, out var u, out var v);
        var (nx, ny, nz) = GetNormal(faceDir);

        var sliceCount = GetDimension(d);
        var uCount = GetDimension(u);
        var vCount = GetDimension(v);

        var mask = ArrayPool<ushort>.Shared.Rent(uCount * vCount);

        try
        {
            for (var slice = 0; slice < sliceCount; slice++)
            {
                Array.Clear(mask, 0, uCount * vCount);

                for (var ui = 0; ui < uCount; ui++)
                for (var vi = 0; vi < vCount; vi++)
                {
                    ResolveCoord(d, u, v, slice, ui, vi, out var px, out var py, out var pz);
                    var blockId = chunk.GetBlock(px, py, pz);
                    if (blockId == 0)
                        continue;

                    var def = blockRegistry.Get(blockId);
                    if (def.IsTransparent && !def.IsLiquid)
                        continue;

                    var neighborBlockId = SampleBlock(chunk, neighbors, px + nx, py + ny, pz + nz);
                    var neighborDef = blockRegistry.Get(neighborBlockId);

                    // Emit face if neighbor is air, or transparent and not the same block type
                    // (prevents interior liquid-to-liquid faces causing z-fighting)
                    if (neighborBlockId == 0 || (neighborDef.IsTransparent && neighborBlockId != blockId))
                    {
                        mask[ui + vi * uCount] = blockId;
                    }
                }

                GreedyMerge(mask, uCount, vCount, faceDir, d, u, v, slice, nx, ny, nz,
                    chunk, neighbors, opaque, liquid);
            }
        }
        finally
        {
            ArrayPool<ushort>.Shared.Return(mask);
        }
    }

    private void GreedyMerge(
        ushort[] mask, int uCount, int vCount,
        int faceDir, int d, int u, int v, int slice,
        int nx, int ny, int nz,
        ChunkData chunk, ChunkData?[] neighbors,
        MeshAccumulator opaque, MeshAccumulator liquid)
    {
        var merged = ArrayPool<bool>.Shared.Rent(uCount * vCount);
        Array.Clear(merged, 0, uCount * vCount);

        try
        {
            for (var vi = 0; vi < vCount; vi++)
            for (var ui = 0; ui < uCount; ui++)
            {
                var idx = ui + vi * uCount;
                if (mask[idx] == 0 || merged[idx])
                    continue;

                var blockId = mask[idx];

                // Expand in u direction
                var width = 1;
                while (ui + width < uCount
                       && mask[(ui + width) + vi * uCount] == blockId
                       && !merged[(ui + width) + vi * uCount])
                    width++;

                // Expand in v direction
                var height = 1;
                var canExpand = true;
                while (canExpand && vi + height < vCount)
                {
                    for (var k = 0; k < width; k++)
                    {
                        var mi = (ui + k) + (vi + height) * uCount;
                        if (mask[mi] != blockId || merged[mi])
                        {
                            canExpand = false;
                            break;
                        }
                    }

                    if (canExpand)
                        height++;
                }

                // Mark as merged
                for (var dv = 0; dv < height; dv++)
                for (var du = 0; du < width; du++)
                    merged[(ui + du) + (vi + dv) * uCount] = true;

                // Route to opaque or liquid accumulator
                var def = blockRegistry.Get(blockId);
                var target = def.IsLiquid ? liquid : opaque;
                var offset = (nx + ny + nz) > 0 ? 1 : 0;

                EmitQuad(d, u, v, slice, offset, ui, vi, width, height,
                    nx, ny, nz, def, faceDir, chunk, neighbors, target);
            }
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(merged);
        }
    }

    private void EmitQuad(
        int d, int u, int v,
        int slice, int offset,
        int ui, int vi, int width, int height,
        int nx, int ny, int nz,
        BlockDefinition def, int faceDir,
        ChunkData chunk, ChunkData?[] neighbors,
        MeshAccumulator target)
    {
        // Corner offsets: (du, dv) for each of the 4 quad vertices
        Span<int> cornerU = stackalloc int[4];
        Span<int> cornerV = stackalloc int[4];

        // Godot uses CW front-face (Vulkan convention). The Z-axis permutation
        // (Z,X,Y) is even, unlike X(X,Z,Y) and Y(Y,X,Z) which are odd.
        // This inverts the Z flip compared to X/Y: flip for -X, -Y, +Z.
        if ((nx + ny - nz) < 0)
        {
            cornerU[0] = 0;      cornerV[0] = 0;
            cornerU[1] = 0;      cornerV[1] = height;
            cornerU[2] = width;  cornerV[2] = height;
            cornerU[3] = width;  cornerV[3] = 0;
        }
        else
        {
            cornerU[0] = 0;      cornerV[0] = 0;
            cornerU[1] = width;  cornerV[1] = 0;
            cornerU[2] = width;  cornerV[2] = height;
            cornerU[3] = 0;      cornerV[3] = height;
        }

        // UV = tiling coords so the shader can fract() per block.
        // UV2 = atlas tile origin.
        float tileU0 = def.FaceUvs[faceDir * 4 + 0];
        float tileV0 = def.FaceUvs[faceDir * 4 + 1];

        // Tiling UV corners must match vertex corners 1:1.
        Span<float> tilingU = stackalloc float[4];
        Span<float> tilingV = stackalloc float[4];

        if ((nx + ny - nz) < 0)
        {
            tilingU[0] = 0;     tilingV[0] = 0;
            tilingU[1] = 0;     tilingV[1] = height;
            tilingU[2] = width; tilingV[2] = height;
            tilingU[3] = width; tilingV[3] = 0;
        }
        else
        {
            tilingU[0] = 0;     tilingV[0] = 0;
            tilingU[1] = width; tilingV[1] = 0;
            tilingU[2] = width; tilingV[2] = height;
            tilingU[3] = 0;     tilingV[3] = height;
        }

        // Side faces (v-axis = Y): flip tiling V so textures aren't upside-down.
        // Godot UV v=0 is top of texture, but world Y=0 is bottom of block.
        if (v == 1)
        {
            for (var i = 0; i < 4; i++)
                tilingV[i] = height - tilingV[i];
        }

        // Compute per-vertex AO. The air level is one step from the solid block
        // in the normal direction.
        var airD = slice + ((nx + ny + nz) > 0 ? 1 : -1);
        Span<float> ao = stackalloc float[4];

        var baseVertex = target.Vertices.Count / 3;

        for (var i = 0; i < 4; i++)
        {
            var du = cornerU[i];
            var dv = cornerV[i];

            ResolveCoord(d, u, v, slice + offset, ui + du, vi + dv,
                out var cx, out var cy, out var cz);

            target.Vertices.Add(cx);
            target.Vertices.Add(cy);
            target.Vertices.Add(cz);

            target.Normals.Add(nx);
            target.Normals.Add(ny);
            target.Normals.Add(nz);

            target.Uvs.Add(tilingU[i]);
            target.Uvs.Add(tilingV[i]);

            target.Uv2s.Add(tileU0);
            target.Uv2s.Add(tileV0);

            // Compute AO for this vertex
            ao[i] = ComputeVertexAO(chunk, neighbors, d, u, v, airD, ui + du, vi + dv, du, dv);

            target.Colors.Add(def.TintR);
            target.Colors.Add(def.TintG);
            target.Colors.Add(def.TintB);
            target.Colors.Add(ao[i]);
        }

        // Quad flip: choose the diagonal that minimizes AO interpolation artifacts.
        // When ao[0]+ao[2] > ao[1]+ao[3], the standard diagonal produces smoother
        // interpolation. Otherwise flip to reduce the visible seam.
        if (ao[0] + ao[2] > ao[1] + ao[3])
        {
            target.Indices.Add(baseVertex);
            target.Indices.Add(baseVertex + 1);
            target.Indices.Add(baseVertex + 2);
            target.Indices.Add(baseVertex);
            target.Indices.Add(baseVertex + 2);
            target.Indices.Add(baseVertex + 3);
        }
        else
        {
            target.Indices.Add(baseVertex + 1);
            target.Indices.Add(baseVertex + 2);
            target.Indices.Add(baseVertex + 3);
            target.Indices.Add(baseVertex + 1);
            target.Indices.Add(baseVertex + 3);
            target.Indices.Add(baseVertex);
        }
    }

    /// <summary>
    /// Computes ambient occlusion for a vertex at position (uVertex, vVertex)
    /// in the face plane. Samples 3 neighboring blocks at the air level:
    /// two edge neighbors and one corner neighbor.
    ///
    /// Returns 0.0 (fully occluded) to 1.0 (fully lit).
    /// Uses the standard voxel AO formula: if both edges are solid, AO = 0.
    /// Otherwise AO = (3 - solidCount) / 3.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float ComputeVertexAO(
        ChunkData chunk, ChunkData?[] neighbors,
        int d, int u, int v,
        int airD, int uVertex, int vVertex,
        int du, int dv)
    {
        // Determine which blocks around this vertex to check.
        // The vertex sits at the corner of 4 blocks. One is known air (the face block).
        // The "other" direction points away from the quad interior.
        var uOther = (du == 0) ? -1 : 0;
        var vOther = (dv == 0) ? -1 : 0;
        var uAir = (du == 0) ? 0 : -1;
        var vAir = (dv == 0) ? 0 : -1;

        var s1 = IsSolidAt(chunk, neighbors, d, u, v, airD, uVertex + uOther, vVertex + vAir);
        var s2 = IsSolidAt(chunk, neighbors, d, u, v, airD, uVertex + uAir, vVertex + vOther);

        if (s1 && s2)
            return 0f;

        var corner = IsSolidAt(chunk, neighbors, d, u, v, airD, uVertex + uOther, vVertex + vOther);
        var count = (s1 ? 1 : 0) + (s2 ? 1 : 0) + (corner ? 1 : 0);
        return (3 - count) / 3f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSolidAt(
        ChunkData chunk, ChunkData?[] neighbors,
        int dAxis, int uAxis, int vAxis,
        int dVal, int uVal, int vVal)
    {
        int x = 0, y = 0, z = 0;
        SetAxis(dAxis, dVal, ref x, ref y, ref z);
        SetAxis(uAxis, uVal, ref x, ref y, ref z);
        SetAxis(vAxis, vVal, ref x, ref y, ref z);

        var blockId = SampleBlock(chunk, neighbors, x, y, z);
        return blockId != 0 && blockRegistry.Get(blockId).IsSolid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SetAxis(int axis, int val, ref int x, ref int y, ref int z)
    {
        switch (axis)
        {
            case 0: x = val; break;
            case 1: y = val; break;
            default: z = val; break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ResolveCoord(int d, int u, int v, int dVal, int uVal, int vVal,
        out int x, out int y, out int z)
    {
        x = 0;
        y = 0;
        z = 0;
        SetAxis(d, dVal, ref x, ref y, ref z);
        SetAxis(u, uVal, ref x, ref y, ref z);
        SetAxis(v, vVal, ref x, ref y, ref z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetDimension(int axis) => axis switch
    {
        0 => ChunkSizeX,
        1 => ChunkSizeY,
        _ => ChunkSizeZ,
    };

    private static ushort SampleBlock(ChunkData main, ChunkData?[] neighbors, int wx, int wy, int wz)
    {
        if (wy is < 0 or >= ChunkData.SizeY)
            return 0;

        if (ChunkData.IsInBounds(wx, wy, wz))
            return main.GetBlock(wx, wy, wz);

        int nx = 0, nz = 0;
        int lx = wx, lz = wz;

        if (wx < 0) { nx = -1; lx = wx + ChunkData.SizeX; }
        else if (wx >= ChunkData.SizeX) { nx = 1; lx = wx - ChunkData.SizeX; }
        if (wz < 0) { nz = -1; lz = wz + ChunkData.SizeZ; }
        else if (wz >= ChunkData.SizeZ) { nz = 1; lz = wz - ChunkData.SizeZ; }

        // neighbors: [0]=+X, [1]=-X, [2]=+Z, [3]=-Z
        ChunkData? neighbor = null;
        if (nx == 1) neighbor = neighbors[0];
        else if (nx == -1) neighbor = neighbors[1];
        else if (nz == 1) neighbor = neighbors[2];
        else if (nz == -1) neighbor = neighbors[3];

        return neighbor?.GetBlock(lx, wy, lz) ?? (ushort)0;
    }

    private static void GetAxes(int faceDir, out int d, out int u, out int v)
    {
        (d, u, v) = faceDir switch
        {
            0 => (0, 2, 1), // +X: d=X, u=Z, v=Y
            1 => (0, 2, 1), // -X
            2 => (1, 0, 2), // +Y: d=Y, u=X, v=Z
            3 => (1, 0, 2), // -Y
            4 => (2, 0, 1), // +Z: d=Z, u=X, v=Y
            5 => (2, 0, 1), // -Z
            _ => throw new ArgumentOutOfRangeException(nameof(faceDir)),
        };
    }

    private static (int Nx, int Ny, int Nz) GetNormal(int faceDir) => faceDir switch
    {
        0 => (1, 0, 0),
        1 => (-1, 0, 0),
        2 => (0, 1, 0),
        3 => (0, -1, 0),
        4 => (0, 0, 1),
        5 => (0, 0, -1),
        _ => throw new ArgumentOutOfRangeException(nameof(faceDir)),
    };

    /// <summary>
    /// Collects mesh vertex data into lists, then converts to MeshData.
    /// Avoids passing 6 lists through the entire call chain.
    /// </summary>
    private sealed class MeshAccumulator(int initialCapacity)
    {
        public readonly List<float> Vertices = new(initialCapacity);
        public readonly List<float> Normals = new(initialCapacity);
        public readonly List<float> Uvs = new(initialCapacity / 2);
        public readonly List<float> Uv2s = new(initialCapacity / 2);
        public readonly List<float> Colors = new(initialCapacity);
        public readonly List<int> Indices = new(initialCapacity * 3 / 2);

        public MeshData ToMeshData()
        {
            if (Vertices.Count == 0)
                return MeshData.Empty;

            return new MeshData(
                Vertices.ToArray(),
                Normals.ToArray(),
                Uvs.ToArray(),
                Uv2s.ToArray(),
                Colors.ToArray(),
                Indices.ToArray());
        }
    }
}
