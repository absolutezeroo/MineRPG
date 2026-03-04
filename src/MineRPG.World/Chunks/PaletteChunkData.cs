using System.Runtime.CompilerServices;

namespace MineRPG.World.Chunks;

/// <summary>
/// Palette-compressed chunk block data. A compact palette of unique block IDs
/// plus a byte array indexing into the palette for each voxel.
/// Memory: palette.Length * 2 + indices.Length bytes (vs indices.Length * 2 raw).
/// </summary>
public sealed class PaletteChunkData(ushort[] palette, byte[] indices)
{
    public ushort[] Palette { get; } = palette;
    public byte[] Indices { get; } = indices;

    public int PaletteSize => Palette.Length;

    /// <summary>
    /// Estimated memory in bytes: palette + indices + overhead.
    /// </summary>
    public int EstimatedBytes => Palette.Length * 2 + Indices.Length + 16;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetBlock(int flatIndex) => Palette[Indices[flatIndex]];
}
