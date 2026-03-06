using MineRPG.RPG.Items;

namespace MineRPG.RPG.Drops;

/// <summary>
/// Represents an item dropped in the world with position, velocity, and age.
/// </summary>
public sealed class DroppedItem
{
    /// <summary>
    /// Creates a new dropped item at the given position.
    /// </summary>
    /// <param name="item">The item instance being dropped.</param>
    /// <param name="worldX">World X position.</param>
    /// <param name="worldY">World Y position.</param>
    /// <param name="worldZ">World Z position.</param>
    /// <param name="velocity">Initial velocity of the drop.</param>
    public DroppedItem(
        ItemInstance item,
        float worldX,
        float worldY,
        float worldZ,
        DropVelocity velocity)
    {
        Item = item ?? throw new ArgumentNullException(nameof(item));
        WorldX = worldX;
        WorldY = worldY;
        WorldZ = worldZ;
        Velocity = velocity;
        Age = 0f;
    }

    /// <summary>The dropped item instance.</summary>
    public ItemInstance Item { get; }

    /// <summary>World X position.</summary>
    public float WorldX { get; set; }

    /// <summary>World Y position.</summary>
    public float WorldY { get; set; }

    /// <summary>World Z position.</summary>
    public float WorldZ { get; set; }

    /// <summary>Current velocity.</summary>
    public DropVelocity Velocity { get; set; }

    /// <summary>Time in seconds since the item was dropped.</summary>
    public float Age { get; set; }
}
