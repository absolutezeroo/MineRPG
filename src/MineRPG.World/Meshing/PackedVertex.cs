using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MineRPG.World.Meshing;

/// <summary>
/// Compressed vertex format for voxel terrain meshes.
/// Packs position, normal, UV, atlas origin, and AO/tint data into 20 bytes
/// instead of the standard 56 bytes (float arrays).
///
/// Layout (20 bytes):
///   [0-1]  ushort PositionX — local X in 1/256 units (0–8191 → 0.0–32.0)
///   [2-3]  ushort PositionY — local Y in 1/256 units
///   [4-5]  ushort PositionZ — local Z in 1/256 units
///   [6]    byte   NormalIndex — face direction 0-5 (6 possible normals)
///   [7]    byte   Padding
///   [8-9]  ushort TileU — tiling UV coordinate U (fixed-point 8.8)
///   [10-11] ushort TileV — tiling UV coordinate V (fixed-point 8.8)
///   [12-13] ushort AtlasU — atlas origin U (fixed-point 0.16)
///   [14-15] ushort AtlasV — atlas origin V (fixed-point 0.16)
///   [16-19] uint   AoTint — AO in bits [0-7], tint RGB565 in bits [8-23], reserved [24-31]
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct PackedVertex
{
    private const float PositionScale = 256f;
    private const float TileUvScale = 256f;
    private const float AtlasUvScale = 65535f;
    private const int TintOffset = 8;

    /// <summary>Packed position X coordinate.</summary>
    public ushort PositionX { get; }

    /// <summary>Packed position Y coordinate.</summary>
    public ushort PositionY { get; }

    /// <summary>Packed position Z coordinate.</summary>
    public ushort PositionZ { get; }

    /// <summary>Normal direction index (0-5).</summary>
    public byte NormalIndex { get; }

    /// <summary>Padding byte for alignment.</summary>
    public byte Reserved { get; }

    /// <summary>Packed tiling UV U coordinate.</summary>
    public ushort TileU { get; }

    /// <summary>Packed tiling UV V coordinate.</summary>
    public ushort TileV { get; }

    /// <summary>Packed atlas origin U coordinate.</summary>
    public ushort AtlasU { get; }

    /// <summary>Packed atlas origin V coordinate.</summary>
    public ushort AtlasV { get; }

    /// <summary>Packed AO and tint color data.</summary>
    public uint AoTint { get; }

    /// <summary>
    /// Creates a packed vertex from uncompressed data.
    /// </summary>
    /// <param name="positionX">World-space X position.</param>
    /// <param name="positionY">World-space Y position.</param>
    /// <param name="positionZ">World-space Z position.</param>
    /// <param name="normalIndex">Face direction index (0-5).</param>
    /// <param name="tileU">Tiling UV U coordinate.</param>
    /// <param name="tileV">Tiling UV V coordinate.</param>
    /// <param name="atlasU">Atlas origin U coordinate (0-1).</param>
    /// <param name="atlasV">Atlas origin V coordinate (0-1).</param>
    /// <param name="ao">Ambient occlusion value (0-1).</param>
    /// <param name="tintR">Tint red channel (0-1).</param>
    /// <param name="tintG">Tint green channel (0-1).</param>
    /// <param name="tintB">Tint blue channel (0-1).</param>
    public PackedVertex(
        float positionX, float positionY, float positionZ,
        byte normalIndex,
        float tileU, float tileV,
        float atlasU, float atlasV,
        float ao,
        float tintR, float tintG, float tintB)
    {
        PositionX = (ushort)(positionX * PositionScale);
        PositionY = (ushort)(positionY * PositionScale);
        PositionZ = (ushort)(positionZ * PositionScale);
        NormalIndex = normalIndex;
        Reserved = 0;
        TileU = (ushort)(tileU * TileUvScale);
        TileV = (ushort)(tileV * TileUvScale);
        AtlasU = (ushort)(atlasU * AtlasUvScale);
        AtlasV = (ushort)(atlasV * AtlasUvScale);

        byte aoByte = (byte)(ao * 255f);
        ushort rgb565 = PackRgb565(tintR, tintG, tintB);
        AoTint = (uint)(aoByte | (rgb565 << TintOffset));
    }

    /// <summary>
    /// Unpacks the position X to a float.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float UnpackPositionX() => PositionX / PositionScale;

    /// <summary>
    /// Unpacks the position Y to a float.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float UnpackPositionY() => PositionY / PositionScale;

    /// <summary>
    /// Unpacks the position Z to a float.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float UnpackPositionZ() => PositionZ / PositionScale;

    /// <summary>
    /// Unpacks the AO value to a float (0-1).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float UnpackAo() => (AoTint & 0xFF) / 255f;

    /// <summary>
    /// Size of a single packed vertex in bytes.
    /// </summary>
    public const int SizeInBytes = 20;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort PackRgb565(float r, float g, float b)
    {
        int red = (int)(r * 31f) & 0x1F;
        int green = (int)(g * 63f) & 0x3F;
        int blue = (int)(b * 31f) & 0x1F;
        return (ushort)((red << 11) | (green << 5) | blue);
    }
}
