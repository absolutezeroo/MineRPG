using System;

namespace MineRPG.World.Spatial;

/// <summary>
/// Immutable integer world-space position. Used for block addressing.
/// </summary>
public readonly struct WorldPosition : IEquatable<WorldPosition>
{
    /// <summary>The zero world position.</summary>
    public static readonly WorldPosition Zero = new(0, 0, 0);

    /// <summary>World X coordinate.</summary>
    public readonly int X;

    /// <summary>World Y coordinate.</summary>
    public readonly int Y;

    /// <summary>World Z coordinate.</summary>
    public readonly int Z;

    /// <summary>
    /// Creates a world position with the given coordinates.
    /// </summary>
    /// <param name="x">World X coordinate.</param>
    /// <param name="y">World Y coordinate.</param>
    /// <param name="z">World Z coordinate.</param>
    public WorldPosition(int x, int y, int z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>
    /// Returns a new position offset by the given deltas.
    /// </summary>
    /// <param name="deltaX">X offset.</param>
    /// <param name="deltaY">Y offset.</param>
    /// <param name="deltaZ">Z offset.</param>
    /// <returns>A new offset world position.</returns>
    public WorldPosition Offset(int deltaX, int deltaY, int deltaZ) =>
        new(X + deltaX, Y + deltaY, Z + deltaZ);

    /// <summary>
    /// Checks equality with another world position.
    /// </summary>
    /// <param name="other">The other position to compare.</param>
    /// <returns>True if all components are equal.</returns>
    public bool Equals(WorldPosition other) => X == other.X && Y == other.Y && Z == other.Z;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is WorldPosition other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    /// <inheritdoc />
    public override string ToString() => $"({X}, {Y}, {Z})";

    /// <summary>Equality operator.</summary>
    public static bool operator ==(WorldPosition left, WorldPosition right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(WorldPosition left, WorldPosition right) => !left.Equals(right);
}
