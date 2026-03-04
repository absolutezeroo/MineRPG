using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace MineRPG.World.Chunks;

/// <summary>
/// Palette compression for chunk block data. Instead of storing a full ushort (16 bits)
/// per voxel, a local palette maps block types to small indices. When a chunk has few
/// distinct block types, this dramatically reduces memory usage.
///
/// A chunk with 5 block types: 65536 voxels x 3 bits = approx. 24 KB (vs 128 KB raw).
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

    private const int InitialPaletteCapacity = 32;
    private const int RawBytesPerBlock = 2;

    /// <summary>
    /// Compresses chunk block data using palette encoding.
    /// Returns null if the chunk has too many distinct block types.
    /// </summary>
    /// <param name="blocks">The raw block ID span to compress.</param>
    /// <returns>Palette-compressed data, or null if compression is not beneficial.</returns>
    public static PaletteChunkData? Compress(ReadOnlySpan<ushort> blocks)
    {
        // Build palette
        Dictionary<ushort, byte> paletteMap = new(InitialPaletteCapacity);
        List<ushort> palette = new(InitialPaletteCapacity);

        for (int i = 0; i < blocks.Length; i++)
        {
            ushort blockId = blocks[i];

            if (!paletteMap.ContainsKey(blockId))
            {
                if (palette.Count >= MaxPaletteSize)
                {
                    return null;
                }

                paletteMap[blockId] = (byte)palette.Count;
                palette.Add(blockId);
            }
        }

        // Encode indices
        byte[] indices = new byte[blocks.Length];

        for (int i = 0; i < blocks.Length; i++)
        {
            indices[i] = paletteMap[blocks[i]];
        }

        return new PaletteChunkData(palette.ToArray(), indices);
    }

    /// <summary>
    /// Decompresses palette-encoded data back to raw ushort block IDs.
    /// </summary>
    /// <param name="compressed">The palette-compressed data.</param>
    /// <param name="output">The output span for decompressed block IDs.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Decompress(PaletteChunkData compressed, Span<ushort> output)
    {
        ushort[] palette = compressed.Palette;
        byte[] indices = compressed.Indices;

        for (int i = 0; i < indices.Length; i++)
        {
            output[i] = palette[indices[i]];
        }
    }

    /// <summary>
    /// Estimates the memory savings of palette compression for given block data.
    /// Returns the ratio (compressed size / raw size), where lower is better.
    /// </summary>
    /// <param name="blocks">The raw block ID span to analyze.</param>
    /// <returns>Compression ratio (0..1), where lower means better compression.</returns>
    public static float EstimateCompressionRatio(ReadOnlySpan<ushort> blocks)
    {
        HashSet<ushort> distinctTypes = new();

        for (int i = 0; i < blocks.Length; i++)
        {
            distinctTypes.Add(blocks[i]);
        }

        if (distinctTypes.Count > MaxPaletteSize)
        {
            return 1.0f;
        }

        int rawBytes = blocks.Length * RawBytesPerBlock;
        int paletteBytes = distinctTypes.Count * RawBytesPerBlock;
        int indexBytes = blocks.Length;
        int compressedBytes = paletteBytes + indexBytes;

        return (float)compressedBytes / rawBytes;
    }
}
