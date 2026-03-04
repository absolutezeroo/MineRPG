namespace MineRPG.Core.Math;

/// <summary>
/// A 2D local coordinate within a chunk (X, Z).
/// </summary>
/// <param name="LocalX">Local X coordinate in [0, chunkSize).</param>
/// <param name="LocalZ">Local Z coordinate in [0, chunkSize).</param>
public readonly record struct LocalCoord2D(int LocalX, int LocalZ);
