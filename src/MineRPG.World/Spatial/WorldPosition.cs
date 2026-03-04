namespace MineRPG.World.Spatial;

/// <summary>
/// Immutable integer world-space position. Used for block addressing.
/// </summary>
public readonly struct WorldPosition(int x, int y, int z) : IEquatable<WorldPosition>
{
    public readonly int X = x;
    public readonly int Y = y;
    public readonly int Z = z;

    public static readonly WorldPosition Zero = new(0, 0, 0);

    public WorldPosition Offset(int dx, int dy, int dz) => new(X + dx, Y + dy, Z + dz);

    public bool Equals(WorldPosition other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is WorldPosition other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"({X}, {Y}, {Z})";

    public static bool operator ==(WorldPosition left, WorldPosition right) => left.Equals(right);
    public static bool operator !=(WorldPosition left, WorldPosition right) => !left.Equals(right);
}
