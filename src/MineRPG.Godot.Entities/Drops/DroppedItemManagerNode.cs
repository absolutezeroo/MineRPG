using System.Collections.Generic;

using Godot;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Godot.UI;
using MineRPG.RPG.Drops;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Godot.Entities.Drops;

/// <summary>
/// Orchestrates all world-dropped items. Spawns <see cref="DroppedItemNode"/>
/// instances in response to <see cref="ItemDropSpawnedEvent"/> and drives
/// the pure-layer <see cref="ItemDropManager"/> each frame.
/// </summary>
public sealed partial class DroppedItemManagerNode : Node3D
{
    private const float MaxDropAge = 300f;
    private const float PickupRadius = 2.0f;

    private readonly Dictionary<DroppedItem, DroppedItemNode> _activeNodes = new();
    private readonly List<DroppedItem> _removedScratch = new();
    private readonly HashSet<DroppedItem> _activeScratch = new(ReferenceEqualityComparer.Instance);

    private ItemDropManager _dropManager = null!;
    private PlayerInventory _playerInventory = null!;
    private ItemRegistry _itemRegistry = null!;
    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;

    private float _playerX;
    private float _playerY;
    private float _playerZ;

    /// <summary>
    /// Wires all dependencies. Called by GameplayBootstrap immediately after AddChild.
    /// </summary>
    /// <param name="dropManager">Pure-layer drop manager.</param>
    /// <param name="playerInventory">The player's inventory for pickup.</param>
    /// <param name="itemRegistry">Registry for definition lookups.</param>
    /// <param name="eventBus">Event bus for subscriptions.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public void Initialize(
        ItemDropManager dropManager,
        PlayerInventory playerInventory,
        ItemRegistry itemRegistry,
        IEventBus eventBus,
        ILogger logger)
    {
        _dropManager = dropManager;
        _playerInventory = playerInventory;
        _itemRegistry = itemRegistry;
        _eventBus = eventBus;
        _logger = logger;

        _eventBus.Subscribe<ItemDropSpawnedEvent>(OnItemDropSpawned);
        _eventBus.Subscribe<PlayerPositionUpdatedEvent>(OnPlayerPositionUpdated);

        _logger.Info("DroppedItemManagerNode: Initialized.");
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (_eventBus is not null)
        {
            _eventBus.Unsubscribe<ItemDropSpawnedEvent>(OnItemDropSpawned);
            _eventBus.Unsubscribe<PlayerPositionUpdatedEvent>(OnPlayerPositionUpdated);
        }
    }

    /// <inheritdoc />
    public override void _Process(double delta)
    {
        if (_dropManager is null)
        {
            return;
        }

        float deltaTime = (float)delta;
        _dropManager.UpdateDrops(deltaTime);

        CollectNearbyDrops();
        RemoveExpiredDrops();
    }

    private void OnItemDropSpawned(ItemDropSpawnedEvent e)
    {
        ItemInstance item = new(e.ItemDefinitionId, e.Count);
        DropVelocity velocity = new(e.VelocityX, e.VelocityY, e.VelocityZ);

        DroppedItem drop = _dropManager.SpawnDrop(e.X, e.Y, e.Z, item, velocity);

        Color color = ResolveItemColor(e.ItemDefinitionId);

        DroppedItemNode node = new();
        node.Name = $"Drop_{e.ItemDefinitionId}_{_activeNodes.Count}";
        AddChild(node);
        node.Initialize(drop, color);

        _activeNodes[drop] = node;

        _logger.Debug(
            "DroppedItemManagerNode: Spawned drop {0} x{1} at ({2:F1},{3:F1},{4:F1})",
            e.ItemDefinitionId, e.Count, e.X, e.Y, e.Z);
    }

    private void OnPlayerPositionUpdated(PlayerPositionUpdatedEvent e)
    {
        _playerX = e.X;
        _playerY = e.Y;
        _playerZ = e.Z;
    }

    private void CollectNearbyDrops()
    {
        IReadOnlyList<ItemInstance> collected = _dropManager.CollectNearby(
            _playerX, _playerY, _playerZ, _playerInventory, PickupRadius);

        for (int i = 0; i < collected.Count; i++)
        {
            _eventBus.Publish(new ItemPickedUpEvent
            {
                ItemDefinitionId = collected[i].DefinitionId,
                Count = collected[i].Count,
            });
        }

        if (collected.Count > 0)
        {
            SyncRemovedNodes();
        }
    }

    private void RemoveExpiredDrops()
    {
        int countBefore = _dropManager.ActiveDrops.Count;
        _dropManager.DespawnExpired(MaxDropAge);

        if (_dropManager.ActiveDrops.Count < countBefore)
        {
            SyncRemovedNodes();
        }
    }

    private void SyncRemovedNodes()
    {
        _removedScratch.Clear();
        _activeScratch.Clear();

        for (int i = 0; i < _dropManager.ActiveDrops.Count; i++)
        {
            _activeScratch.Add(_dropManager.ActiveDrops[i]);
        }

        foreach (DroppedItem drop in _activeNodes.Keys)
        {
            if (!_activeScratch.Contains(drop))
            {
                _removedScratch.Add(drop);
            }
        }

        for (int i = 0; i < _removedScratch.Count; i++)
        {
            if (_activeNodes.TryGetValue(_removedScratch[i], out DroppedItemNode? node))
            {
                node.QueueFree();
                _activeNodes.Remove(_removedScratch[i]);
            }
        }
    }

    private Color ResolveItemColor(string definitionId)
    {
        if (_itemRegistry.TryGet(definitionId, out ItemDefinition definition))
        {
            return GameTheme.GetCategoryPlaceholderColor(definition.Category);
        }

        return Colors.White;
    }
}
