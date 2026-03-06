using System;

namespace MineRPG.Core.Math;

/// <summary>
/// Immutable value-type coordinate identifying a chunk in the world grid.
/// Used as dictionary key and in spatial queries.
/// </summary>
public readonly struct ChunkCoord(int x, int z) : IEquatable<ChunkCoord>
{
    /// <summary>
    /// The chunk X coordinate in the world grid.
    /// </summary>
    public readonly int X = x;

    /// <summary>
    /// The chunk Z coordinate in the world grid.
    /// </summary>
    public readonly int Z = z;

    /// <summary>
    /// The origin chunk at (0, 0).
    /// </summary>
    public static readonly ChunkCoord Zero = new(0, 0);

    /// <summary>
    /// The chunk immediately to the north (Z - 1).
    /// </summary>
    public ChunkCoord North => new(X, Z - 1);

    /// <summary>
    /// The chunk immediately to the south (Z + 1).
    /// </summary>
    public ChunkCoord South => new(X, Z + 1);

    /// <summary>
    /// The chunk immediately to the east (X + 1).
    /// </summary>
    public ChunkCoord East => new(X + 1, Z);

    /// <summary>
    /// The chunk immediately to the west (X - 1).
    /// </summary>
    public ChunkCoord West => new(X - 1, Z);

    /// <summary>
    /// Chebyshev (max-norm) distance - matches a square view distance shape.
    /// </summary>
    /// <param name="other">The other chunk coordinate to measure distance to.</param>
    /// <returns>The Chebyshev distance between this chunk and the other.</returns>
    public int ChebyshevDistance(ChunkCoord other)
        => System.Math.Max(System.Math.Abs(X - other.X), System.Math.Abs(Z - other.Z));

    /// <inheritdoc />
    public bool Equals(ChunkCoord other) => X == other.X && Z == other.Z;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ChunkCoord other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Z);

    /// <inheritdoc />
    public override string ToString() => $"ChunkCoord({X}, {Z})";

    /// <summary>
    /// Determines whether two <see cref="ChunkCoord"/> values are equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if the two coordinates are equal.</returns>
    public static bool operator ==(ChunkCoord left, ChunkCoord right) => left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="ChunkCoord"/> values are not equal.
    /// </summary>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <returns>True if the two coordinates are not equal.</returns>
    public static bool operator !=(ChunkCoord left, ChunkCoord right) => !left.Equals(right);
}
