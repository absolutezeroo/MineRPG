using System.Runtime.CompilerServices;

namespace MineRPG.World.Chunks.Serialization;

/// <summary>
/// Palette-compressed chunk block data. A compact palette of unique block IDs
/// plus a byte array indexing into the palette for each voxel.
/// Memory: palette.Length * 2 + indices.Length bytes (vs indices.Length * 2 raw).
/// </summary>
public sealed class PaletteChunkData
{
    private const int PaletteEntrySize = 2;
    private const int OverheadBytes = 16;

    /// <summary>Array of unique block IDs referenced by the indices.</summary>
    public ushort[] Palette { get; }

    /// <summary>Per-voxel index into the palette array.</summary>
    public byte[] Indices { get; }

    /// <summary>Number of unique block types in the palette.</summary>
    public int PaletteSize => Palette.Length;

    /// <summary>
    /// Estimated memory in bytes: palette + indices + overhead.
    /// </summary>
    public int EstimatedBytes => Palette.Length * PaletteEntrySize + Indices.Length + OverheadBytes;

    /// <summary>
    /// Creates palette-compressed chunk data from pre-built palette and indices.
    /// </summary>
    /// <param name="palette">The palette of unique block IDs.</param>
    /// <param name="indices">Per-voxel palette indices.</param>
    public PaletteChunkData(ushort[] palette, byte[] indices)
    {
        Palette = palette;
        Indices = indices;
    }

    /// <summary>
    /// Gets the block ID at the given flat index.
    /// </summary>
    /// <param name="flatIndex">The flat voxel index.</param>
    /// <returns>The block ID.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort GetBlock(int flatIndex) => Palette[Indices[flatIndex]];
}
