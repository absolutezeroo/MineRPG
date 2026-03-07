using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.RPG.Drops;

/// <summary>
/// Manages item drops in the world: spawning, collecting, and despawning.
/// </summary>
public sealed class ItemDropManager
{
    private const float DefaultPickupRadius = 2.0f;
    private const float DefaultMaxAge = 300f;

    private readonly List<DroppedItem> _drops = new();
    private readonly ItemRegistry _itemRegistry;
    private readonly ITerrainQuery? _terrainQuery;

    /// <summary>
    /// Creates an item drop manager.
    /// </summary>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    /// <param name="terrainQuery">
    /// Optional terrain query for drop physics. When null, drops remain
    /// stationary (age-only updates). When provided, gravity and velocity
    /// are applied each frame.
    /// </param>
    public ItemDropManager(ItemRegistry itemRegistry, ITerrainQuery? terrainQuery = null)
    {
        _itemRegistry = itemRegistry ?? throw new ArgumentNullException(nameof(itemRegistry));
        _terrainQuery = terrainQuery;
    }

    /// <summary>All currently active drops in the world.</summary>
    public IReadOnlyList<DroppedItem> ActiveDrops => _drops;

    /// <summary>
    /// Spawns an item drop at the given world position.
    /// </summary>
    /// <param name="worldX">World X coordinate.</param>
    /// <param name="worldY">World Y coordinate.</param>
    /// <param name="worldZ">World Z coordinate.</param>
    /// <param name="item">The item to drop.</param>
    /// <param name="velocity">Initial drop velocity.</param>
    /// <returns>The newly created dropped item.</returns>
    public DroppedItem SpawnDrop(
        float worldX,
        float worldY,
        float worldZ,
        ItemInstance item,
        DropVelocity velocity)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        DroppedItem drop = new DroppedItem(item, worldX, worldY, worldZ, velocity);
        _drops.Add(drop);
        return drop;
    }

    /// <summary>
    /// Collects nearby drops into the player's inventory.
    /// </summary>
    /// <param name="playerX">Player's world X position.</param>
    /// <param name="playerY">Player's world Y position.</param>
    /// <param name="playerZ">Player's world Z position.</param>
    /// <param name="inventory">The player's inventory to add items to.</param>
    /// <param name="pickupRadius">Radius within which drops are collected.</param>
    /// <returns>Items that were successfully collected.</returns>
    public IReadOnlyList<ItemInstance> CollectNearby(
        float playerX,
        float playerY,
        float playerZ,
        PlayerInventory inventory,
        float pickupRadius = DefaultPickupRadius)
    {
        if (inventory == null)
        {
            throw new ArgumentNullException(nameof(inventory));
        }

        List<ItemInstance> collected = new();
        float radiusSquared = pickupRadius * pickupRadius;

        for (int i = _drops.Count - 1; i >= 0; i--)
        {
            DroppedItem drop = _drops[i];

            float deltaX = drop.WorldX - playerX;
            float deltaY = drop.WorldY - playerY;
            float deltaZ = drop.WorldZ - playerZ;
            float distanceSquared = (deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ);

            if (distanceSquared > radiusSquared)
            {
                continue;
            }

            int remaining = inventory.AddItem(drop.Item);

            if (remaining <= 0)
            {
                collected.Add(drop.Item);
                _drops.RemoveAt(i);
            }
        }

        return collected;
    }

    /// <summary>
    /// Removes drops that have exceeded the maximum age.
    /// </summary>
    /// <param name="maxAge">Maximum age in seconds before despawning.</param>
    public void DespawnExpired(float maxAge = DefaultMaxAge)
    {
        for (int i = _drops.Count - 1; i >= 0; i--)
        {
            if (_drops[i].Age >= maxAge)
            {
                _drops.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Updates all active drops: advances age and applies physics when
    /// a terrain query is available.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since last update.</param>
    public void UpdateDrops(float deltaTime)
    {
        for (int i = 0; i < _drops.Count; i++)
        {
            DroppedItem drop = _drops[i];
            drop.Age += deltaTime;

            if (_terrainQuery is not null)
            {
                float surfaceY = _terrainQuery.GetSurfaceY(drop.WorldX, drop.WorldZ);
                ItemDropPhysics.Step(drop, deltaTime, surfaceY);
            }
        }
    }
}
