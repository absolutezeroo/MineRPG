using System.Runtime.CompilerServices;

namespace MineRPG.World.Chunks;

/// <summary>
/// Palette compression for chunk block data. Instead of storing a full ushort (16 bits)
/// per voxel, a local palette maps block types to small indices. When a chunk has few
/// distinct block types, this dramatically reduces memory usage.
///
/// A chunk with 5 block types: 65536 voxels × 3 bits ≈ 24 KB (vs 128 KB raw).
///
/// Thread-safe: methods operate on provided buffers, no shared state.
/// </summary>
public static class PaletteCompressor
{
    /// <summary>
    /// Maximum palette entries before falling back to raw storage.
    /// At 256 entries each index fits in a byte, halving memory vs ushort.
    /// </summary>
    public const int MaxPaletteSize = 256;

    /// <summary>
    /// Compresses chunk block data using palette encoding.
    /// Returns null if the chunk has too many distinct block types.
    /// </summary>
    public static PaletteChunkData? Compress(ReadOnlySpan<ushort> blocks)
    {
        // Build palette
        var paletteMap = new Dictionary<ushort, byte>(32);
        var palette = new List<ushort>(32);

        for (var i = 0; i < blocks.Length; i++)
        {
            var blockId = blocks[i];
            if (!paletteMap.ContainsKey(blockId))
            {
                if (palette.Count >= MaxPaletteSize)
                    return null;

                paletteMap[blockId] = (byte)palette.Count;
                palette.Add(blockId);
            }
        }

        // Encode indices
        var indices = new byte[blocks.Length];
        for (var i = 0; i < blocks.Length; i++)
        {
            indices[i] = paletteMap[blocks[i]];
        }

        return new PaletteChunkData(palette.ToArray(), indices);
    }

    /// <summary>
    /// Decompresses palette-encoded data back to raw ushort block IDs.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Decompress(PaletteChunkData compressed, Span<ushort> output)
    {
        var palette = compressed.Palette;
        var indices = compressed.Indices;

        for (var i = 0; i < indices.Length; i++)
        {
            output[i] = palette[indices[i]];
        }
    }

    /// <summary>
    /// Estimates the memory savings of palette compression for given block data.
    /// Returns the ratio (compressed size / raw size), where lower is better.
    /// </summary>
    public static float EstimateCompressionRatio(ReadOnlySpan<ushort> blocks)
    {
        var distinctTypes = new HashSet<ushort>();
        for (var i = 0; i < blocks.Length; i++)
            distinctTypes.Add(blocks[i]);

        if (distinctTypes.Count > MaxPaletteSize)
            return 1.0f;

        var rawBytes = blocks.Length * 2; // ushort = 2 bytes
        var paletteBytes = distinctTypes.Count * 2; // palette entries
        var indexBytes = blocks.Length; // byte per index
        var compressedBytes = paletteBytes + indexBytes;

        return (float)compressedBytes / rawBytes;
    }
}

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
