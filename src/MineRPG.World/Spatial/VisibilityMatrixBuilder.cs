using MineRPG.World.Chunks;

namespace MineRPG.World.Spatial;

/// <summary>
/// Computes a <see cref="ChunkVisibilityMatrix"/> for a chunk column by running
/// flood-fill BFS from each of the 4 horizontal faces. Uses a per-sub-chunk
/// approach: runs BFS independently per 16x16x16 sub-chunk via
/// <see cref="SubChunkFloodFill"/>, then combines results.
/// Thread-safe: all state is local to each Build() call.
/// </summary>
public static class VisibilityMatrixBuilder
{
    private const int SubSize = SubChunkConstants.SubChunkSize;

    /// <summary>
    /// Builds the face-to-face visibility matrix for a full chunk column.
    /// Examines each non-empty, non-fully-solid sub-chunk independently.
    /// </summary>
    /// <param name="chunk">The chunk data to analyze.</param>
    /// <param name="subChunkInfos">Pre-computed sub-chunk metadata (may be null).</param>
    /// <returns>The combined visibility matrix for the chunk column.</returns>
    public static ChunkVisibilityMatrix Build(ChunkData chunk, SubChunkInfo[]? subChunkInfos)
    {
        ushort combinedBits = 0;

        for (int subChunkIndex = 0; subChunkIndex < SubChunkConstants.SubChunkCount; subChunkIndex++)
        {
            if (subChunkInfos is not null)
            {
                SubChunkInfo info = subChunkInfos[subChunkIndex];

                if (info.IsEmpty)
                {
                    combinedBits |= 0xFFFF;
                    continue;
                }

                if (info.IsFullySolid)
                {
                    continue;
                }
            }

            int minY = subChunkIndex * SubSize;
            ushort subBits = BuildSubChunk(chunk, minY);
            combinedBits |= subBits;

            if (combinedBits == 0xFFFF)
            {
                return ChunkVisibilityMatrix.AllVisible;
            }
        }

        return new ChunkVisibilityMatrix(combinedBits);
    }

    /// <summary>
    /// Runs BFS for a single 16x16x16 sub-chunk section starting at minY.
    /// Pre-checks for air on each face before running the full flood fill.
    /// </summary>
    private static ushort BuildSubChunk(ChunkData chunk, int minY)
    {
        ushort bits = 0;

        bool hasNorthAir = false;
        bool hasSouthAir = false;
        bool hasEastAir = false;
        bool hasWestAir = false;

        for (int y = minY; y < minY + SubSize; y++)
        {
            for (int u = 0; u < ChunkData.SizeX; u++)
            {
                if (chunk.GetBlock(u, y, 0) == 0)
                {
                    hasNorthAir = true;
                }

                if (chunk.GetBlock(u, y, ChunkData.SizeZ - 1) == 0)
                {
                    hasSouthAir = true;
                }
            }

            for (int u = 0; u < ChunkData.SizeZ; u++)
            {
                if (chunk.GetBlock(ChunkData.SizeX - 1, y, u) == 0)
                {
                    hasEastAir = true;
                }

                if (chunk.GetBlock(0, y, u) == 0)
                {
                    hasWestAir = true;
                }
            }
        }

        if (hasNorthAir)
        {
            bits |= SubChunkFloodFill.FloodFromFace(chunk, minY, ChunkVisibilityMatrix.FaceNorth);
        }

        if (hasSouthAir)
        {
            bits |= SubChunkFloodFill.FloodFromFace(chunk, minY, ChunkVisibilityMatrix.FaceSouth);
        }

        if (hasEastAir)
        {
            bits |= SubChunkFloodFill.FloodFromFace(chunk, minY, ChunkVisibilityMatrix.FaceEast);
        }

        if (hasWestAir)
        {
            bits |= SubChunkFloodFill.FloodFromFace(chunk, minY, ChunkVisibilityMatrix.FaceWest);
        }

        return bits;
    }
}
