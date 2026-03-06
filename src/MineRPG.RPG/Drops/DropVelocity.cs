namespace MineRPG.RPG.Drops;

/// <summary>
/// Initial velocity of a dropped item entity in the world.
/// </summary>
public readonly struct DropVelocity
{
    /// <summary>Velocity for items popping out of a broken block.</summary>
    public static readonly DropVelocity BlockBreak = new(0f, 0.3f, 0f);

    /// <summary>Velocity for items dropping from a killed mob.</summary>
    public static readonly DropVelocity MobDeath = new(0f, 0.5f, 0f);

    /// <summary>Velocity for items thrown by the player.</summary>
    public static readonly DropVelocity PlayerThrow = new(0f, 0.2f, 1f);

    /// <summary>X velocity component.</summary>
    public float X { get; init; }

    /// <summary>Y velocity component (vertical).</summary>
    public float Y { get; init; }

    /// <summary>Z velocity component.</summary>
    public float Z { get; init; }

    /// <summary>
    /// Creates a drop velocity with the specified components.
    /// </summary>
    /// <param name="x">X velocity.</param>
    /// <param name="y">Y velocity (vertical).</param>
    /// <param name="z">Z velocity.</param>
    public DropVelocity(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
