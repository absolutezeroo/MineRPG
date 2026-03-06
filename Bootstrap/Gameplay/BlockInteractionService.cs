using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Godot.World;
using MineRPG.RPG.Items;
using MineRPG.World.Blocks;
using MineRPG.World.Mining;
using MineRPG.World.Spatial;

namespace MineRPG.Game.Bootstrap.Gameplay;

/// <summary>
/// Implements progressive block mining and block placement.
/// Lives in MineRPG.Game where all project references are available.
/// Mining advances each physics frame while the attack button is held.
/// </summary>
public sealed class BlockInteractionService : IBlockInteractionService
{
    /// <summary>Half-width of the player capsule on X/Z axes.</summary>
    private const float PlayerHalfWidth = 0.3f;

    /// <summary>Half-height of the player capsule on Y axis.</summary>
    private const float PlayerHalfHeight = 0.9f;

    private readonly IVoxelRaycaster _raycaster;
    private readonly WorldNode _worldNode;
    private readonly BlockRegistry _blockRegistry;
    private readonly PlayerData _playerData;
    private readonly MiningState _miningState;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;

    /// <summary>Default RPG modifier when no stat system is connected.</summary>
    private const float DefaultMiningSpeedModifier = 1f;

    private string _equippedToolType;
    private int _equippedToolTier;
    private float _toolSpeedMultiplier;

    private float _currentMineTime;
    private bool _currentIsCorrectTool;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlockInteractionService"/> class.
    /// </summary>
    /// <param name="raycaster">Voxel raycaster for block hit detection.</param>
    /// <param name="worldNode">World node for breaking and placing blocks.</param>
    /// <param name="blockRegistry">Block definition registry.</param>
    /// <param name="playerData">Player state container.</param>
    /// <param name="miningState">Mining progress state tracker.</param>
    /// <param name="eventBus">Event bus for publishing mining events.</param>
    /// <param name="equippedTool">Currently equipped tool, or null for bare hands.</param>
    /// <param name="logger">Logger instance.</param>
    public BlockInteractionService(
        IVoxelRaycaster raycaster,
        WorldNode worldNode,
        BlockRegistry blockRegistry,
        PlayerData playerData,
        MiningState miningState,
        IEventBus eventBus,
        ToolDefinition? equippedTool,
        ILogger logger)
    {
        _raycaster = raycaster;
        _worldNode = worldNode;
        _blockRegistry = blockRegistry;
        _playerData = playerData;
        _miningState = miningState;
        _eventBus = eventBus;
        _logger = logger;

        _equippedToolType = equippedTool?.ToolType ?? "";
        _equippedToolTier = equippedTool?.ToolTier ?? 0;
        _toolSpeedMultiplier = equippedTool?.SpeedMultiplier ?? 1f;
    }

    /// <summary>
    /// Updates the currently equipped tool. Call this when the player
    /// equips or unequips a tool. Pass null for bare hands.
    /// Cancels any active mining operation since the tool changed.
    /// </summary>
    /// <param name="tool">The new tool definition, or null for bare hands.</param>
    public void UpdateEquippedTool(ToolDefinition? tool)
    {
        CancelMining();

        _equippedToolType = tool?.ToolType ?? "";
        _equippedToolTier = tool?.ToolTier ?? 0;
        _toolSpeedMultiplier = tool?.SpeedMultiplier ?? 1f;
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

        if (!_miningState.IsTargeting(hitX, hitY, hitZ))
        {
            if (_miningState.IsActive)
            {
                PublishCancelEvent();
            }

            _miningState.Start(hitX, hitY, hitZ);

            _currentIsCorrectTool = MiningCalculator.IsCorrectTool(
                block, _equippedToolType, _equippedToolTier);

            _currentMineTime = MiningCalculator.ComputeMineTime(
                block, _equippedToolType, _equippedToolTier,
                _toolSpeedMultiplier, DefaultMiningSpeedModifier);

            _logger.Debug(
                "Mining started at ({0},{1},{2}), block={3}, mineTime={4:F2}s, correctTool={5}",
                hitX, hitY, hitZ, block.Name, _currentMineTime, _currentIsCorrectTool);
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
                hitX, hitY, hitZ, minedBlockId);
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
        float maxDistance, ushort blockId)
    {
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

        _worldNode.PlaceBlock(target, blockId);
        return true;
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
