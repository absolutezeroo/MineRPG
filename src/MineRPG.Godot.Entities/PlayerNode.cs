using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Interfaces.Lifecycle;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Entities.Player.Survival;
using MineRPG.Game.Bootstrap.Gameplay;
using MineRPG.Game.Bootstrap.Input;

namespace MineRPG.Godot.Entities;

/// <summary>
/// Godot bridge for the player. Thin orchestrator around
/// <see cref="PlayerCameraController"/>, <see cref="PlayerMovementController"/>,
/// <see cref="PlayerInteractionController"/>, and <see cref="PlayerPositionPublisher"/>.
/// </summary>
public sealed partial class PlayerNode : CharacterBody3D
{
    /// <summary>
    /// Safe margin for collision detection. Prevents minor penetration
    /// and tunneling when collision shapes are rebuilt asynchronously.
    /// </summary>
    private const float PhysicsSafeMargin = 0.01f;

    /// <summary>Floor snap length for walking mode. Disabled in fly mode.</summary>
    private const float DefaultFloorSnapLength = 0.1f;

    [Export] private Camera3D _camera = null!;

    private PlayerData _playerData = null!;
    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private bool _isMouseCaptured;

    private PlayerCameraController _cameraController = null!;
    private PlayerMovementController _movementController = null!;
    private PlayerInteractionController _interactionController = null!;
    private PlayerPositionPublisher _positionPublisher = null!;
    private PlayerEnvironmentSensor? _environmentSensor;
    private ItemConsumptionService? _consumptionService;

    /// <inheritdoc />
    public override void _Ready()
    {
        _playerData = ServiceLocator.Instance.Get<PlayerData>();
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        SafeMargin = PhysicsSafeMargin;
        FloorConstantSpeed = true;
        FloorSnapLength = DefaultFloorSnapLength;
        FloorStopOnSlope = true;

        _camera ??= GetNode<Camera3D>("Camera3D");

        IBlockInteractionService? blockInteraction = null;

        if (ServiceLocator.Instance.TryGet<IBlockInteractionService>(out IBlockInteractionService? resolved))
        {
            blockInteraction = resolved;
        }

        _cameraController = new PlayerCameraController(_playerData, _camera);
        _movementController = new PlayerMovementController(_playerData, _logger);
        _interactionController = new PlayerInteractionController(
            _playerData, _camera, _logger, blockInteraction);
        _positionPublisher = new PlayerPositionPublisher(_eventBus);

        Position = new Vector3(_playerData.PositionX, _playerData.PositionY, _playerData.PositionZ);
        Velocity = Vector3.Zero;
        Rotation = new Vector3(0f, _playerData.CameraYaw, 0f);
        _camera.Rotation = new Vector3(_playerData.CameraPitch, 0f, 0f);

        _environmentSensor = PlayerEnvironmentSensor.TryCreate(_playerData, _logger);

        if (ServiceLocator.Instance.TryGet<ItemConsumptionService>(out ItemConsumptionService? consumption))
        {
            _consumptionService = consumption;
        }

        ProcessMode = ProcessModeEnum.Disabled;
        _eventBus.Subscribe<WorldReadyEvent>(OnWorldReady);
        _eventBus.Subscribe<PlayerRespawnedEvent>(OnPlayerRespawned);

        _logger.Info("PlayerNode ready at {0} (frozen, awaiting WorldReadyEvent).", Position);
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        _eventBus?.Unsubscribe<WorldReadyEvent>(OnWorldReady);
        _eventBus?.Unsubscribe<PlayerRespawnedEvent>(OnPlayerRespawned);
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (_playerData is null || _camera is null)
        {
            return;
        }

        if (@event is InputEventMouseMotion mouseMotion &&
            Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _cameraController.HandleMouseMotion(mouseMotion, this);
        }

        if (@event.IsActionPressed(InputActions.Pause))
        {
            _eventBus.Publish(new PauseRequestedEvent());
        }

        if (@event.IsActionPressed(InputActions.Interact) &&
            Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            // Try consuming held item first; fall through to block placement if not consumable
            if (_consumptionService is null || !_consumptionService.TryConsumeHeldItem())
            {
                _interactionController.TryPlaceBlock();
            }
        }

        _movementController.HandleFlyInput(@event, this);
    }

    /// <inheritdoc />
    public override void _PhysicsProcess(double delta)
    {
        if (_playerData is null)
        {
            return;
        }

        float deltaTime = (float)delta;

        if (_playerData.IsFlying)
        {
            _movementController.ProcessFlyMovement(this);
        }
        else
        {
            _movementController.ProcessWalkMovement(this, deltaTime);
        }

        MoveAndSlide();

        _playerData.PositionX = Position.X;
        _playerData.PositionY = Position.Y;
        _playerData.PositionZ = Position.Z;

        _positionPublisher.PublishIfMoved(Position.X, Position.Y, Position.Z);

        _environmentSensor?.Tick(Position);
        _playerData.Survival?.Tick(deltaTime);

        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            _interactionController.TickMiningInput(deltaTime);
        }
    }

    private void OnWorldReady(WorldReadyEvent evt)
    {
        _eventBus.Unsubscribe<WorldReadyEvent>(OnWorldReady);
        ProcessMode = ProcessModeEnum.Inherit;
        CaptureMouse();

        _positionPublisher.SeedPosition(Position.X, Position.Y, Position.Z);

        _eventBus.Publish(new PlayerPositionUpdatedEvent
        {
            X = Position.X,
            Y = Position.Y,
            Z = Position.Z,
        });

        _logger.Info("PlayerNode: WorldReady - gameplay started at {0}.", Position);
    }

    private void OnPlayerRespawned(PlayerRespawnedEvent evt)
    {
        Position = new Vector3(evt.SpawnX, evt.SpawnY, evt.SpawnZ);
        Velocity = Vector3.Zero;
        _logger.Info("PlayerNode: Respawned at ({0:F1}, {1:F1}, {2:F1}).", evt.SpawnX, evt.SpawnY, evt.SpawnZ);
    }

    private void CaptureMouse()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _isMouseCaptured = true;
    }

    private void ReleaseMouse()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _isMouseCaptured = false;
    }
}
