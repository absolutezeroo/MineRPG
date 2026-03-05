using System;

using MineRPG.World.Chunks;
using MineRPG.World.Generation;

namespace MineRPG.World.Biomes;

/// <summary>
/// Stores biome assignments for a chunk at 4x4x4 resolution (matching Minecraft).
/// A 16x256x16 chunk has 4x64x4 = 1024 biome cells.
/// Biome varies both horizontally and vertically (cave biomes below surface).
/// </summary>
public sealed class BiomeColumn
{
    /// <summary>Resolution of the biome grid (blocks per biome cell).</summary>
    public const int CellSize = 4;

    /// <summary>Number of cells along X.</summary>
    public const int CellsX = ChunkData.SizeX / CellSize;

    /// <summary>Number of cells along Y.</summary>
    public const int CellsY = ChunkData.SizeY / CellSize;

    /// <summary>Number of cells along Z.</summary>
    public const int CellsZ = ChunkData.SizeZ / CellSize;

    /// <summary>Total biome cells per chunk.</summary>
    public const int TotalCells = CellsX * CellsY * CellsZ;

    private readonly string[] _biomeIds;

    /// <summary>
    /// Creates a biome column with all cells set to the given default biome.
    /// </summary>
    /// <param name="defaultBiomeId">Default biome ID for all cells.</param>
    public BiomeColumn(string defaultBiomeId)
    {
        _biomeIds = new string[TotalCells];

        for (int i = 0; i < TotalCells; i++)
        {
            _biomeIds[i] = defaultBiomeId;
        }
    }

    /// <summary>
    /// Gets the biome ID at the given cell coordinates.
    /// </summary>
    /// <param name="cellX">Cell X in [0, CellsX).</param>
    /// <param name="cellY">Cell Y in [0, CellsY).</param>
    /// <param name="cellZ">Cell Z in [0, CellsZ).</param>
    /// <returns>The biome ID at the cell.</returns>
    public string GetBiomeId(int cellX, int cellY, int cellZ) => _biomeIds[GetIndex(cellX, cellY, cellZ)];

    /// <summary>
    /// Gets the biome ID at a block position (converts to cell coordinates).
    /// </summary>
    /// <param name="localX">Local block X in [0, 16).</param>
    /// <param name="y">Block Y in [0, 256).</param>
    /// <param name="localZ">Local block Z in [0, 16).</param>
    /// <returns>The biome ID at the block position.</returns>
    public string GetBiomeIdAtBlock(int localX, int y, int localZ)
    {
        int cellX = Math.Clamp(localX / CellSize, 0, CellsX - 1);
        int cellY = Math.Clamp(y / CellSize, 0, CellsY - 1);
        int cellZ = Math.Clamp(localZ / CellSize, 0, CellsZ - 1);
        return _biomeIds[GetIndex(cellX, cellY, cellZ)];
    }

    /// <summary>
    /// Sets the biome ID at the given cell coordinates.
    /// </summary>
    /// <param name="cellX">Cell X in [0, CellsX).</param>
    /// <param name="cellY">Cell Y in [0, CellsY).</param>
    /// <param name="cellZ">Cell Z in [0, CellsZ).</param>
    /// <param name="biomeId">The biome ID to set.</param>
    public void SetBiomeId(int cellX, int cellY, int cellZ, string biomeId) => _biomeIds[GetIndex(cellX, cellY, cellZ)] = biomeId;

    /// <summary>
    /// Sets all cells in a vertical column (all Y levels) to the given biome.
    /// </summary>
    /// <param name="cellX">Cell X in [0, CellsX).</param>
    /// <param name="cellZ">Cell Z in [0, CellsZ).</param>
    /// <param name="biomeId">The biome ID to set.</param>
    public void SetSurfaceBiome(int cellX, int cellZ, string biomeId)
    {
        for (int cellY = 0; cellY < CellsY; cellY++)
        {
            _biomeIds[GetIndex(cellX, cellY, cellZ)] = biomeId;
        }
    }

    /// <summary>
    /// Sets biome for cells below a given Y level to a cave biome.
    /// </summary>
    /// <param name="cellX">Cell X in [0, CellsX).</param>
    /// <param name="cellZ">Cell Z in [0, CellsZ).</param>
    /// <param name="surfaceCellY">Cell Y of the surface.</param>
    /// <param name="caveBiomeId">Biome ID for underground cells.</param>
    public void SetCaveBiome(int cellX, int cellZ, int surfaceCellY, string caveBiomeId)
    {
        for (int cellY = 0; cellY < surfaceCellY; cellY++)
        {
            _biomeIds[GetIndex(cellX, cellY, cellZ)] = caveBiomeId;
        }
    }

    /// <summary>
    /// Returns the raw biome ID array for serialization.
    /// </summary>
    /// <returns>A read-only span of all biome IDs.</returns>
    public ReadOnlySpan<string> GetRawSpan() => _biomeIds.AsSpan();

    private static int GetIndex(int cellX, int cellY, int cellZ) => cellX + cellZ * CellsX + cellY * CellsX * CellsZ;
}
