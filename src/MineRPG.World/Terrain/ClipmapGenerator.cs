using System;
using System.Collections.Generic;

using MineRPG.World.Chunks;
using MineRPG.World.Meshing;

namespace MineRPG.World.Terrain;

/// <summary>
/// Generates a 2D heightmap mesh for each clipmap ring. Samples the noise
/// height function directly (no chunk data needed) to create a terrain
/// silhouette at the horizon. Each ring is a single mesh with vertices
/// spaced by <see cref="ClipmapRing.BlocksPerVertex"/> blocks.
///
/// The caller provides a height sampling delegate so this class has no
/// dependency on the specific noise implementation.
///
/// Thread-safe: all state is local to each Generate() call.
/// </summary>
public static class ClipmapGenerator
{
    /// <summary>
    /// Delegate for sampling terrain height at a world XZ coordinate.
    /// Returns the surface Y in blocks.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <returns>The surface height in blocks.</returns>
    public delegate float HeightSampler(float worldX, float worldZ);

    /// <summary>
    /// Delegate for sampling biome color at a world XZ coordinate.
    /// Returns RGBA as 4 floats (r, g, b, a) packed in order.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <param name="r">Red channel (0-1).</param>
    /// <param name="g">Green channel (0-1).</param>
    /// <param name="b">Blue channel (0-1).</param>
    public delegate void ColorSampler(float worldX, float worldZ, out float r, out float g, out float b);

    /// <summary>
    /// Generates mesh data for all clipmap rings centered at the given world position.
    /// </summary>
    /// <param name="config">Clipmap configuration.</param>
    /// <param name="centerWorldX">Player world X coordinate.</param>
    /// <param name="centerWorldZ">Player world Z coordinate.</param>
    /// <param name="sampleHeight">Height sampling function.</param>
    /// <param name="sampleColor">Biome color sampling function.</param>
    /// <returns>Array of mesh data, one per ring. Empty meshes for skipped rings.</returns>
    public static MeshData[] Generate(
        ClipmapConfig config,
        float centerWorldX,
        float centerWorldZ,
        HeightSampler sampleHeight,
        ColorSampler sampleColor)
    {
        MeshData[] ringMeshes = new MeshData[ClipmapConfig.RingCount];

        for (int ringIndex = 0; ringIndex < config.Rings.Length && ringIndex < ClipmapConfig.RingCount; ringIndex++)
        {
            ringMeshes[ringIndex] = GenerateRing(
                config.Rings[ringIndex], centerWorldX, centerWorldZ,
                sampleHeight, sampleColor);
        }

        // Fill remaining slots with empty if fewer rings defined
        for (int i = config.Rings.Length; i < ClipmapConfig.RingCount; i++)
        {
            ringMeshes[i] = MeshData.Empty;
        }

        return ringMeshes;
    }

    private static MeshData GenerateRing(
        ClipmapRing ring,
        float centerWorldX,
        float centerWorldZ,
        HeightSampler sampleHeight,
        ColorSampler sampleColor)
    {
        int step = ring.BlocksPerVertex;
        float innerRadius = ring.InnerRadiusChunks * ChunkData.SizeX;
        float outerRadius = ring.OuterRadiusChunks * ChunkData.SizeX;

        // Snap center to grid
        float snappedX = MathF.Floor(centerWorldX / step) * step;
        float snappedZ = MathF.Floor(centerWorldZ / step) * step;

        // Grid bounds covering the outer radius
        int gridRadius = (int)(outerRadius / step) + 1;
        int gridSize = gridRadius * 2 + 1;

        // First pass: compute vertex positions and collect valid vertices
        List<float> vertexList = new(gridSize * gridSize * 3);
        List<float> normalList = new(gridSize * gridSize * 3);
        List<float> colorList = new(gridSize * gridSize * 4);
        List<float> uvList = new(gridSize * gridSize * 2);
        List<float> uv2List = new(gridSize * gridSize * 2);
        List<int> indexList = new(gridSize * gridSize * 6);

        // Vertex index grid (to build triangle indices)
        int[] vertexIndices = new int[gridSize * gridSize];
        Array.Fill(vertexIndices, -1);

        int vertexCount = 0;

        for (int gz = 0; gz < gridSize; gz++)
        {
            for (int gx = 0; gx < gridSize; gx++)
            {
                float worldX = snappedX + (gx - gridRadius) * step;
                float worldZ = snappedZ + (gz - gridRadius) * step;
                float distanceSquared = (worldX - centerWorldX) * (worldX - centerWorldX)
                                       + (worldZ - centerWorldZ) * (worldZ - centerWorldZ);

                float innerSq = innerRadius * innerRadius;
                float outerSq = outerRadius * outerRadius;

                if (distanceSquared < innerSq * 1.05f || distanceSquared > outerSq * 1.1f)
                {
                    continue;
                }

                float height = sampleHeight(worldX, worldZ);
                sampleColor(worldX, worldZ, out float red, out float green, out float blue);

                vertexIndices[gx + gz * gridSize] = vertexCount;
                vertexCount++;

                vertexList.Add(worldX);
                vertexList.Add(height);
                vertexList.Add(worldZ);

                normalList.Add(0f);
                normalList.Add(1f);
                normalList.Add(0f);

                uvList.Add(worldX / (outerRadius * 2f));
                uvList.Add(worldZ / (outerRadius * 2f));

                uv2List.Add(0f);
                uv2List.Add(0f);

                colorList.Add(red);
                colorList.Add(green);
                colorList.Add(blue);
                colorList.Add(1f);
            }
        }

        if (vertexCount < 3)
        {
            return MeshData.Empty;
        }

        // Build triangle indices from the grid
        for (int gz = 0; gz < gridSize - 1; gz++)
        {
            for (int gx = 0; gx < gridSize - 1; gx++)
            {
                int topLeft = vertexIndices[gx + gz * gridSize];
                int topRight = vertexIndices[(gx + 1) + gz * gridSize];
                int bottomLeft = vertexIndices[gx + (gz + 1) * gridSize];
                int bottomRight = vertexIndices[(gx + 1) + (gz + 1) * gridSize];

                if (topLeft < 0 || topRight < 0 || bottomLeft < 0 || bottomRight < 0)
                {
                    continue;
                }

                // Triangle 1: TL, BL, TR
                indexList.Add(topLeft);
                indexList.Add(bottomLeft);
                indexList.Add(topRight);

                // Triangle 2: TR, BL, BR
                indexList.Add(topRight);
                indexList.Add(bottomLeft);
                indexList.Add(bottomRight);
            }
        }

        if (indexList.Count == 0)
        {
            return MeshData.Empty;
        }

        return new MeshData(
            vertexList.ToArray(),
            normalList.ToArray(),
            uvList.ToArray(),
            uv2List.ToArray(),
            colorList.ToArray(),
            indexList.ToArray());
    }
}
