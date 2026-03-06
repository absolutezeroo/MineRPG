using System;

namespace MineRPG.World.Meshing;

/// <summary>
/// Converts standard MeshData (float arrays) into an array of <see cref="PackedVertex"/>
/// for reduced memory and improved GPU cache utilization. The packed format is ~64% smaller.
///
/// Also provides unpacking for debugging or when the packed shader is not available.
///
/// Thread-safe: all state is local to each method call.
/// </summary>
public static class VertexPacker
{
    private const int VertexStride = 3;
    private const int UvStride = 2;
    private const int ColorStride = 4;

    /// <summary>
    /// Packs a MeshData into an array of compressed vertices.
    /// Normal directions are quantized to the nearest axis-aligned direction.
    /// </summary>
    /// <param name="meshData">The source mesh data to pack.</param>
    /// <returns>An array of packed vertices. Empty array if input is empty.</returns>
    public static PackedVertex[] Pack(MeshData meshData)
    {
        if (meshData.IsEmpty)
        {
            return [];
        }

        PackedVertex[] packed = new PackedVertex[meshData.VertexCount];

        for (int i = 0; i < meshData.VertexCount; i++)
        {
            int vBase = i * VertexStride;
            int uvBase = i * UvStride;
            int colorBase = i * ColorStride;

            float posX = meshData.Vertices[vBase];
            float posY = meshData.Vertices[vBase + 1];
            float posZ = meshData.Vertices[vBase + 2];

            float normX = meshData.Normals[vBase];
            float normY = meshData.Normals[vBase + 1];
            float normZ = meshData.Normals[vBase + 2];
            byte normalIndex = QuantizeNormal(normX, normY, normZ);

            float tileU = meshData.Uvs[uvBase];
            float tileV = meshData.Uvs[uvBase + 1];
            float atlasU = meshData.Uv2s[uvBase];
            float atlasV = meshData.Uv2s[uvBase + 1];

            float red = meshData.Colors[colorBase];
            float green = meshData.Colors[colorBase + 1];
            float blue = meshData.Colors[colorBase + 2];
            float ao = meshData.Colors[colorBase + 3];

            packed[i] = new PackedVertex(
                posX, posY, posZ, normalIndex,
                tileU, tileV, atlasU, atlasV,
                ao, red, green, blue);
        }

        return packed;
    }

    /// <summary>
    /// Unpacks packed vertices back to standard MeshData for debugging.
    /// Some precision loss occurs due to the compression.
    /// </summary>
    /// <param name="packedVertices">The packed vertex array.</param>
    /// <param name="indices">The triangle index array (unchanged).</param>
    /// <returns>Reconstructed MeshData.</returns>
    public static MeshData Unpack(PackedVertex[] packedVertices, int[] indices)
    {
        if (packedVertices.Length == 0)
        {
            return MeshData.Empty;
        }

        int count = packedVertices.Length;
        float[] vertices = new float[count * 3];
        float[] normals = new float[count * 3];
        float[] uvs = new float[count * 2];
        float[] uv2s = new float[count * 2];
        float[] colors = new float[count * 4];

        for (int i = 0; i < count; i++)
        {
            PackedVertex packed = packedVertices[i];
            int vBase = i * 3;
            int uvBase = i * 2;
            int colorBase = i * 4;

            vertices[vBase] = packed.UnpackPositionX();
            vertices[vBase + 1] = packed.UnpackPositionY();
            vertices[vBase + 2] = packed.UnpackPositionZ();

            (float nX, float nY, float nZ) = DequantizeNormal(packed.NormalIndex);
            normals[vBase] = nX;
            normals[vBase + 1] = nY;
            normals[vBase + 2] = nZ;

            uvs[uvBase] = packed.TileU / 256f;
            uvs[uvBase + 1] = packed.TileV / 256f;
            uv2s[uvBase] = packed.AtlasU / 65535f;
            uv2s[uvBase + 1] = packed.AtlasV / 65535f;

            float ao = packed.UnpackAo();
            colors[colorBase] = 1f;
            colors[colorBase + 1] = 1f;
            colors[colorBase + 2] = 1f;
            colors[colorBase + 3] = ao;
        }

        return new MeshData(vertices, normals, uvs, uv2s, colors, indices);
    }

    /// <summary>
    /// Quantizes a normal vector to the nearest axis-aligned direction index.
    /// 0=+X, 1=-X, 2=+Y, 3=-Y, 4=+Z, 5=-Z.
    /// </summary>
    private static byte QuantizeNormal(float x, float y, float z)
    {
        float absX = MathF.Abs(x);
        float absY = MathF.Abs(y);
        float absZ = MathF.Abs(z);

        if (absX >= absY && absX >= absZ)
        {
            return x >= 0 ? (byte)0 : (byte)1;
        }

        if (absY >= absX && absY >= absZ)
        {
            return y >= 0 ? (byte)2 : (byte)3;
        }

        return z >= 0 ? (byte)4 : (byte)5;
    }

    /// <summary>
    /// Converts a normal index back to a unit normal vector.
    /// </summary>
    private static (float X, float Y, float Z) DequantizeNormal(byte index)
    {
        return index switch
        {
            0 => (1f, 0f, 0f),
            1 => (-1f, 0f, 0f),
            2 => (0f, 1f, 0f),
            3 => (0f, -1f, 0f),
            4 => (0f, 0f, 1f),
            5 => (0f, 0f, -1f),
            _ => (0f, 1f, 0f),
        };
    }
}
