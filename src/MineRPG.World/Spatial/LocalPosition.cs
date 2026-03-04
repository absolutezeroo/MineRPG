namespace MineRPG.World.Spatial;

/// <summary>
/// Position local to a chunk, byte-sized components in [0, ChunkSize).
/// </summary>
public readonly struct LocalPosition(byte x, byte y, byte z) : IEquatable<LocalPosition>
{
    public readonly byte X = x;
    public readonly byte Y = y;
    public readonly byte Z = z;

    public bool Equals(LocalPosition other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is LocalPosition other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"({X}, {Y}, {Z})";

    public static bool operator ==(LocalPosition left, LocalPosition right) => left.Equals(right);
    public static bool operator !=(LocalPosition left, LocalPosition right) => !left.Equals(right);
}
