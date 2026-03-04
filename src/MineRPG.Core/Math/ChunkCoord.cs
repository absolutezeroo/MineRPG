namespace MineRPG.Core.Math;

/// <summary>
/// Immutable value-type coordinate identifying a chunk in the world grid.
/// Used as dictionary key and in spatial queries.
/// </summary>
public readonly struct ChunkCoord(int x, int z) : IEquatable<ChunkCoord>
{
    public readonly int X = x;
    public readonly int Z = z;

    public static readonly ChunkCoord Zero = new(0, 0);

    public ChunkCoord North => new(X, Z - 1);
    public ChunkCoord South => new(X, Z + 1);
    public ChunkCoord East => new(X + 1, Z);
    public ChunkCoord West => new(X - 1, Z);

    /// <summary>
    /// Chebyshev (max-norm) distance — matches a square view distance shape.
    /// </summary>
    public int ChebyshevDistance(ChunkCoord other)
        => System.Math.Max(System.Math.Abs(X - other.X), System.Math.Abs(Z - other.Z));

    public bool Equals(ChunkCoord other) => X == other.X && Z == other.Z;

    public override bool Equals(object? obj) => obj is ChunkCoord other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(X, Z);

    public override string ToString() => $"ChunkCoord({X}, {Z})";

    public static bool operator ==(ChunkCoord left, ChunkCoord right) => left.Equals(right);

    public static bool operator !=(ChunkCoord left, ChunkCoord right) => !left.Equals(right);
}
