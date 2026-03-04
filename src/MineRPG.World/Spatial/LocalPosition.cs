using System;

namespace MineRPG.World.Spatial;

/// <summary>
/// Position local to a chunk, byte-sized components in [0, ChunkSize).
/// </summary>
public readonly struct LocalPosition : IEquatable<LocalPosition>
{
    /// <summary>Local X coordinate within the chunk.</summary>
    public readonly byte X;

    /// <summary>Local Y coordinate within the chunk.</summary>
    public readonly byte Y;

    /// <summary>Local Z coordinate within the chunk.</summary>
    public readonly byte Z;

    /// <summary>
    /// Creates a local position with the given coordinates.
    /// </summary>
    /// <param name="x">Local X coordinate.</param>
    /// <param name="y">Local Y coordinate.</param>
    /// <param name="z">Local Z coordinate.</param>
    public LocalPosition(byte x, byte y, byte z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Checks equality with another local position.
    /// </summary>
    /// <param name="other">The other position to compare.</param>
    /// <returns>True if all components are equal.</returns>
    public bool Equals(LocalPosition other) => X == other.X && Y == other.Y && Z == other.Z;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is LocalPosition other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y}, {Z})";

    /// <summary>Equality operator.</summary>
    public static bool operator ==(LocalPosition left, LocalPosition right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(LocalPosition left, LocalPosition right) => !left.Equals(right);
}
