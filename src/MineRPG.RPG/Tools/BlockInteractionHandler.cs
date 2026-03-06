using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.RPG.Drops;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.RPG.Tools;

/// <summary>
/// Orchestrates player interactions with blocks: mining, placing, and using items.
/// Point of entry for left-click (mine) and right-click (place/use) actions.
/// </summary>
public sealed class BlockInteractionHandler
{
    private readonly MiningProgressTracker _miningTracker;
    private readonly ItemRegistry _itemRegistry;
    private readonly IEventBus _eventBus;

    private float _currentBreakTime;
    private string? _pendingDropItemId;
    private int _pendingDropCount;
    private int _pendingExperienceDrop;

    /// <summary>
    /// Creates a block interaction handler.
    /// </summary>
    /// <param name="itemRegistry">The item registry.</param>
    /// <param name="eventBus">The event bus for publishing interaction events.</param>
    public BlockInteractionHandler(ItemRegistry itemRegistry, IEventBus eventBus)
    {
        _itemRegistry = itemRegistry ?? throw new ArgumentNullException(nameof(itemRegistry));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _miningTracker = new MiningProgressTracker();
    }

    /// <summary>The underlying mining progress tracker.</summary>
    public MiningProgressTracker MiningTracker => _miningTracker;

    /// <summary>
    /// Starts or continues mining the targeted block.
    /// </summary>
    /// <param name="targetX">Target block X coordinate.</param>
    /// <param name="targetY">Target block Y coordinate.</param>
    /// <param name="targetZ">Target block Z coordinate.</param>
    /// <param name="blockHardness">Hardness of the target block.</param>
    /// <param name="blockRequiredToolType">Required tool type, or null.</param>
    /// <param name="blockMinToolTier">Minimum tool tier for drops.</param>
    /// <param name="dropItemId">Item ID dropped when block is broken.</param>
    /// <param name="dropCount">Number of items dropped.</param>
    /// <param name="experienceDrop">XP gained from breaking.</param>
    /// <param name="inventory">The player's inventory.</param>
    /// <param name="breakTime">Pre-calculated break time in seconds.</param>
    public void StartMining(
        int targetX,
        int targetY,
        int targetZ,
        float blockHardness,
        string? blockRequiredToolType,
        int blockMinToolTier,
        string? dropItemId,
        int dropCount,
        int experienceDrop,
        PlayerInventory inventory,
        float breakTime)
    {
        Core.Math.VoxelPosition3D targetBlock = new(targetX, targetY, targetZ);

        if (_miningTracker.IsMining && _miningTracker.TargetBlock == targetBlock)
        {
            return;
        }

        _miningTracker.CancelMining();

        _currentBreakTime = breakTime;
        _pendingDropItemId = dropItemId;
        _pendingDropCount = dropCount;
        _pendingExperienceDrop = experienceDrop;

        _miningTracker.StartMining(targetBlock, breakTime);
    }

    /// <summary>
    /// Updates mining progress. Called each frame while the player holds the mine button.
    /// </summary>
    /// <param name="deltaTime">Time since last frame.</param>
    /// <param name="inventory">The player's inventory for tool durability.</param>
    /// <param name="dropManager">The drop manager for spawning drops.</param>
    /// <param name="blockWorldX">World X of the target block.</param>
    /// <param name="blockWorldY">World Y of the target block.</param>
    /// <param name="blockWorldZ">World Z of the target block.</param>
    /// <param name="blockId">The block type ID being mined.</param>
    public void UpdateMining(
        float deltaTime,
        PlayerInventory inventory,
        ItemDropManager dropManager,
        float blockWorldX,
        float blockWorldY,
        float blockWorldZ,
        ushort blockId)
    {
        if (!_miningTracker.IsMining)
        {
            return;
        }

        float previousProgress = _miningTracker.Progress;
        _miningTracker.UpdateMining(deltaTime);

        if (_miningTracker.Progress != previousProgress)
        {
            _eventBus.Publish(new MiningProgressChangedEvent
            {
                Progress = _miningTracker.Progress,
            });
        }

        if (_miningTracker.IsComplete() || !_miningTracker.IsMining)
        {
            OnBlockBroken(inventory, dropManager, blockWorldX, blockWorldY, blockWorldZ, blockId);
        }
    }

    /// <summary>
    /// Cancels the current mining operation.
    /// </summary>
    public void CancelMining()
    {
        _miningTracker.CancelMining();
    }

    /// <summary>
    /// Handles right-click to place a block from the held item.
    /// </summary>
    /// <param name="adjacentX">X coordinate of the placement position.</param>
    /// <param name="adjacentY">Y coordinate of the placement position.</param>
    /// <param name="adjacentZ">Z coordinate of the placement position.</param>
    /// <param name="inventory">The player's inventory.</param>
    /// <returns>The block ID to place, or null if no block item is held.</returns>
    public string? TryPlaceBlock(
        int adjacentX,
        int adjacentY,
        int adjacentZ,
        PlayerInventory inventory)
    {
        ItemInstance? heldItem = inventory.SelectedItem;

        if (heldItem == null)
        {
            return null;
        }

        if (!_itemRegistry.TryGet(heldItem.DefinitionId, out ItemDefinition definition))
        {
            return null;
        }

        if (string.IsNullOrEmpty(definition.PlacesBlockId))
        {
            return null;
        }

        // Consume one item from the stack
        heldItem.Count -= 1;

        if (heldItem.Count <= 0)
        {
            inventory.Hotbar.RemoveAt(inventory.SelectedHotbarIndex, 1);
        }

        _eventBus.Publish(new BlockPlacedEvent
        {
            X = adjacentX,
            Y = adjacentY,
            Z = adjacentZ,
            ItemId = heldItem.DefinitionId,
        });

        return definition.PlacesBlockId;
    }

    private void OnBlockBroken(
        PlayerInventory inventory,
        ItemDropManager dropManager,
        float blockWorldX,
        float blockWorldY,
        float blockWorldZ,
        ushort blockId)
    {
        // Spawn drops
        if (!string.IsNullOrEmpty(_pendingDropItemId) && _pendingDropCount > 0)
        {
            ItemInstance dropItem = new ItemInstance(_pendingDropItemId!, _pendingDropCount);
            dropManager.SpawnDrop(blockWorldX, blockWorldY, blockWorldZ, dropItem, DropVelocity.BlockBreak);
        }

        // Damage tool durability
        ItemInstance? heldTool = inventory.SelectedItem;

        if (heldTool != null && heldTool.HasDurability)
        {
            heldTool.DamageDurability(1);

            if (heldTool.IsBroken)
            {
                _eventBus.Publish(new ToolBrokenEvent
                {
                    ItemId = heldTool.DefinitionId,
                    SlotIndex = inventory.SelectedHotbarIndex,
                });

                inventory.Hotbar.RemoveAt(inventory.SelectedHotbarIndex, heldTool.Count);
            }
        }

        // Publish block mined event
        Core.Math.VoxelPosition3D target = _miningTracker.TargetBlock;

        _eventBus.Publish(new BlockMinedEvent
        {
            X = target.X,
            Y = target.Y,
            Z = target.Z,
            BlockId = blockId,
            UsedCorrectTool = true,
        });

        _pendingDropItemId = null;
        _pendingDropCount = 0;
        _pendingExperienceDrop = 0;
    }
}
