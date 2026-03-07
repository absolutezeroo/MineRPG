using System;
using System.Collections.Generic;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Godot.World;
using MineRPG.RPG.Drops;
using MineRPG.RPG.Items;
using MineRPG.RPG.Loot;
using MineRPG.World.Blocks;
using MineRPG.World.Mining;
using MineRPG.World.Spatial;

namespace MineRPG.Game.Bootstrap.Gameplay;

/// <summary>
/// Implements progressive block mining and block placement.
/// Lives in MineRPG.Game where all project references are available.
/// Mining advances each physics frame while the attack button is held.
/// Reads the equipped tool from <see cref="HotbarController"/> each tick.
/// </summary>
public sealed class BlockInteractionService : IBlockInteractionService
{
    /// <summary>Half-width of the player capsule on X/Z axes.</summary>
    private const float PlayerHalfWidth = 0.3f;

    /// <summary>Half-height of the player capsule on Y axis.</summary>
    private const float PlayerHalfHeight = 0.9f;

    /// <summary>Default RPG modifier when no stat system is connected.</summary>
    private const float DefaultMiningSpeedModifier = 1f;

    private readonly IVoxelRaycaster _raycaster;
    private readonly WorldNode _worldNode;
    private readonly BlockRegistry _blockRegistry;
    private readonly ItemRegistry _itemRegistry;
    private readonly LootResolver _lootResolver;
    private readonly HotbarController _hotbarController;
    private readonly PlayerData _playerData;
    private readonly MiningState _miningState;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;
    private readonly Random _random = new();

    private float _currentMineTime;
    private bool _currentIsCorrectTool;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockInteractionService"/> class.
    /// </summary>
    /// <param name="raycaster">Voxel raycaster for block hit detection.</param>
    /// <param name="worldNode">World node for breaking and placing blocks.</param>
    /// <param name="blockRegistry">Block definition registry.</param>
    /// <param name="itemRegistry">Item definition registry for resolving placeable blocks.</param>
    /// <param name="lootResolver">Loot resolver for determining block drops.</param>
    /// <param name="hotbarController">Hotbar controller for reading the equipped tool.</param>
    /// <param name="playerData">Player state container.</param>
    /// <param name="miningState">Mining progress state tracker.</param>
    /// <param name="eventBus">Event bus for publishing mining events.</param>
    /// <param name="logger">Logger instance.</param>
    public BlockInteractionService(
        IVoxelRaycaster raycaster,
        WorldNode worldNode,
        BlockRegistry blockRegistry,
        ItemRegistry itemRegistry,
        LootResolver lootResolver,
        HotbarController hotbarController,
        PlayerData playerData,
        MiningState miningState,
        IEventBus eventBus,
        ILogger logger)
    {
        _raycaster = raycaster;
        _worldNode = worldNode;
        _blockRegistry = blockRegistry;
        _itemRegistry = itemRegistry;
        _lootResolver = lootResolver;
        _hotbarController = hotbarController;
        _playerData = playerData;
        _miningState = miningState;
        _eventBus = eventBus;
        _logger = logger;
    }

    /// <inheritdoc />
    public void TickMining(
        float originX, float originY, float originZ,
        float dirX, float dirY, float dirZ,
        float maxDistance, float deltaTime)
    {
        VoxelRaycastResult result = _raycaster.Cast(
            originX, originY, originZ, dirX, dirY, dirZ, maxDistance);

        if (!result.Hit)
        {
            CancelMining();
            return;
        }

        BlockDefinition block = _blockRegistry.Get(result.BlockId);

        if (block.Hardness < 0f)
        {
            CancelMining();
            return;
        }

        int hitX = result.HitPosition.X;
        int hitY = result.HitPosition.Y;
        int hitZ = result.HitPosition.Z;

        // Read the equipped tool each tick (player can change slot at any time)
        ToolProperties? tool = _hotbarController.GetSelectedToolProperties();
        string equippedToolType = tool?.ToolType.ToString().ToUpperInvariant() ?? "";
        int equippedToolTier = tool?.HarvestLevel ?? 0;
        float toolSpeed = tool?.MiningSpeed ?? 1f;

        if (!_miningState.IsTargeting(hitX, hitY, hitZ))
        {
            if (_miningState.IsActive)
            {
                PublishCancelEvent();
            }

            _miningState.Start(hitX, hitY, hitZ);

            _currentIsCorrectTool = MiningCalculator.IsCorrectTool(
                block, equippedToolType, equippedToolTier);

            _currentMineTime = MiningCalculator.ComputeMineTime(
                block, equippedToolType, equippedToolTier,
                toolSpeed, DefaultMiningSpeedModifier);

            _logger.Debug(
                "Mining started at ({0},{1},{2}), block={3}, mineTime={4:F2}s, correctTool={5}",
                hitX, hitY, hitZ, block.DisplayName, _currentMineTime, _currentIsCorrectTool);
        }

        _miningState.Advance(deltaTime, _currentMineTime);

        _eventBus.Publish(new MiningProgressChangedEvent
        {
            X = hitX,
            Y = hitY,
            Z = hitZ,
            Progress = _miningState.Progress,
            CrackStage = _miningState.CrackStage,
            IsActive = true,
        });

        if (_miningState.IsComplete)
        {
            ushort minedBlockId = result.BlockId;
            _worldNode.BreakBlock(result.HitPosition);

            DropItemsFromBlock(block);
            DamageEquippedTool();

            _eventBus.Publish(new BlockMinedEvent
            {
                X = hitX,
                Y = hitY,
                Z = hitZ,
                BlockId = minedBlockId,
                UsedCorrectTool = _currentIsCorrectTool,
            });

            _miningState.Cancel();

            _logger.Debug(
                "Block mined at ({0},{1},{2}), blockId={3}",
                hitX, hitY, hitZ, block.Id);
        }
    }

    /// <inheritdoc />
    public void CancelMining()
    {
        if (!_miningState.IsActive)
        {
            return;
        }

        PublishCancelEvent();
        _miningState.Cancel();
    }

    /// <inheritdoc />
    public bool TryPlaceBlock(
        float originX, float originY, float originZ,
        float dirX, float dirY, float dirZ,
        float maxDistance)
    {
        ItemInstance? heldItem = _hotbarController.GetSelectedItem();

        if (heldItem is null)
        {
            return false;
        }

        if (!_itemRegistry.TryGet(heldItem.DefinitionId, out ItemDefinition itemDef))
        {
            return false;
        }

        if (string.IsNullOrEmpty(itemDef.LinkedBlockId))
        {
            return false;
        }

        if (!_blockRegistry.TryGet(itemDef.LinkedBlockId, out BlockDefinition block))
        {
            _logger.Warning(
                "TryPlaceBlock: item '{0}' linked block '{1}' not found in BlockRegistry.",
                heldItem.DefinitionId, itemDef.LinkedBlockId);
            return false;
        }

        VoxelRaycastResult result = _raycaster.Cast(
            originX, originY, originZ, dirX, dirY, dirZ, maxDistance);

        if (!result.Hit)
        {
            return false;
        }

        WorldPosition target = result.AdjacentPosition;

        if (BlockOverlapsPlayer(target))
        {
            _logger.Debug("TryPlaceBlock: rejected - block at {0} overlaps player", target);
            return false;
        }

        _worldNode.PlaceBlock(target, block.RuntimeId);

        _playerData.Inventory?.Hotbar.RemoveAt(_playerData.SelectedHotbarSlot, 1);

        return true;
    }

    /// <summary>
    /// Resolves drops from the loot table system and spawns world drops.
    /// </summary>
    private void DropItemsFromBlock(BlockDefinition block)
    {
        if (string.IsNullOrEmpty(block.LootTableId))
        {
            return;
        }

        IReadOnlyList<ItemInstance> drops = _lootResolver.Resolve(block.LootTableId, _random);

        for (int i = 0; i < drops.Count; i++)
        {
            ItemInstance drop = drops[i];

            _eventBus.Publish(new ItemDropSpawnedEvent
            {
                X = _miningState.TargetX + 0.5f,
                Y = _miningState.TargetY + 0.5f,
                Z = _miningState.TargetZ + 0.5f,
                ItemDefinitionId = drop.DefinitionId,
                Count = drop.Count,
                VelocityX = DropVelocity.BlockBreak.X,
                VelocityY = DropVelocity.BlockBreak.Y,
                VelocityZ = DropVelocity.BlockBreak.Z,
            });
        }
    }

    /// <summary>
    /// Damages the currently equipped tool by 1 durability point.
    /// Removes the tool from the hotbar if it breaks.
    /// </summary>
    private void DamageEquippedTool()
    {
        ItemInstance? tool = _hotbarController.GetSelectedItem();

        if (tool is null || !tool.HasDurability)
        {
            return;
        }

        tool.DamageDurability(1);

        if (tool.IsBroken)
        {
            _playerData.Inventory?.Hotbar.RemoveAt(_playerData.SelectedHotbarSlot, 1);

            _eventBus.Publish(new ToolBrokenEvent
            {
                ItemId = tool.DefinitionId,
                SlotIndex = _playerData.SelectedHotbarSlot,
            });

            _logger.Info("Tool broken: {0}", tool.DefinitionId);
        }
    }

    private void PublishCancelEvent()
    {
        _eventBus.Publish(new MiningProgressChangedEvent
        {
            X = _miningState.TargetX,
            Y = _miningState.TargetY,
            Z = _miningState.TargetZ,
            Progress = 0f,
            CrackStage = 0,
            IsActive = false,
        });
    }

    /// <summary>
    /// Checks whether a block at the given world position would overlap the player's body.
    /// Uses an AABB approximation of the player capsule (radius 0.3, height 1.8).
    /// </summary>
    private bool BlockOverlapsPlayer(WorldPosition blockPos)
    {
        float px = _playerData.PositionX;
        float py = _playerData.PositionY;
        float pz = _playerData.PositionZ;

        int bx = blockPos.X;
        int by = blockPos.Y;
        int bz = blockPos.Z;

        return bx < px + PlayerHalfWidth && px - PlayerHalfWidth < bx + 1
            && by < py + PlayerHalfHeight && py - PlayerHalfHeight < by + 1
            && bz < pz + PlayerHalfWidth && pz - PlayerHalfWidth < bz + 1;
    }
}
