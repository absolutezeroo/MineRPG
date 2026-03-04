namespace MineRPG.Core.Math;

/// <summary>
/// A 2D chunk coordinate pair (X, Z).
/// </summary>
/// <param name="ChunkX">Chunk X coordinate.</param>
/// <param name="ChunkZ">Chunk Z coordinate.</param>
public readonly record struct ChunkCoord2D(int ChunkX, int ChunkZ);
