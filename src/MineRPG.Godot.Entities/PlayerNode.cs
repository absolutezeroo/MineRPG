using System;

using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Lifecycle;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;

namespace MineRPG.Godot.Entities;

/// <summary>
/// Godot bridge for the player. Thin wrapper around PlayerData.
/// Handles first-person CharacterBody3D movement, mouse look,
/// and block break/place via IBlockInteractionService.
/// </summary>
public sealed partial class PlayerNode : CharacterBody3D
{
    private const float MinPitchRadians = -89f * MathF.PI / 180f;
    private const float MaxPitchRadians = 89f * MathF.PI / 180f;
    private const float PositionPublishThresholdSquared = 0.0001f;

    /// <summary>
    /// Safe margin for collision detection. Prevents minor penetration
    /// and tunneling when collision shapes are rebuilt asynchronously.
    /// </summary>
    private const float PhysicsSafeMargin = 0.01f;

    [Export] private Camera3D _camera = null!;

    private PlayerData _playerData = null!;
    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;
    private IBlockInteractionService? _blockInteraction;
    private bool _isMouseCaptured;
    private float _lastPublishedX;
    private float _lastPublishedY;
    private float _lastPublishedZ;

    /// <inheritdoc />
    public override void _Ready()
    {
        _playerData = ServiceLocator.Instance.Get<PlayerData>();
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        // CharacterBody3D physics tuning for voxel terrain
        SafeMargin = PhysicsSafeMargin;
        FloorConstantSpeed = true;
        FloorSnapLength = 0.1f;
        FloorStopOnSlope = true;

        // Resolve camera -- [Export] NodePath may not auto-resolve on private fields
        _camera ??= GetNode<Camera3D>("Camera3D");

        if (ServiceLocator.Instance.TryGet<IBlockInteractionService>(out IBlockInteractionService? blockInteraction))
        {
            _blockInteraction = blockInteraction;
        }

        // Restore saved position and camera orientation from PlayerData.
        // CompositionRoot.Wire() populates PlayerData from the save file before
        // ChangeSceneToFile() is called, so these values are ready.
        // Velocity is zeroed — restoring mid-air velocity before terrain is meshed
        // would cause the player to fall through the world.
        Position = new Vector3(_playerData.PositionX, _playerData.PositionY, _playerData.PositionZ);
        Velocity = Vector3.Zero;
        Rotation = new Vector3(0f, _playerData.CameraYaw, 0f);
        _camera.Rotation = new Vector3(_playerData.CameraPitch, 0f, 0f);

        // Freeze until terrain is preloaded — prevents physics falling before chunks exist.
        // Mouse capture is deferred to OnWorldReady so the cursor stays visible during loading.
        ProcessMode = ProcessModeEnum.Disabled;
        _eventBus.Subscribe<WorldReadyEvent>(OnWorldReady);

        _logger.Info("PlayerNode ready at {0} (frozen, awaiting WorldReadyEvent).", Position);
    }

    /// <inheritdoc />
    public override void _ExitTree() => _eventBus?.Unsubscribe<WorldReadyEvent>(OnWorldReady);

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (_playerData is null || _camera is null)
        {
            return;
        }

        if (@event is InputEventMouseMotion mouseMotion && _isMouseCaptured)
        {
            float sensitivity = _playerData.MovementSettings.MouseSensitivity;
            _playerData.CameraYaw -= mouseMotion.Relative.X * sensitivity;
            _playerData.CameraPitch -= mouseMotion.Relative.Y * sensitivity;
            _playerData.CameraPitch = Mathf.Clamp(_playerData.CameraPitch, MinPitchRadians, MaxPitchRadians);

            Rotation = new Vector3(0, _playerData.CameraYaw, 0);
            _camera.Rotation = new Vector3(_playerData.CameraPitch, 0, 0);
        }

        if (@event.IsActionPressed(InputActions.Pause))
        {
            if (ServiceLocator.Instance.TryGet<IGameStateController>(
                out IGameStateController? controller))
            {
                controller.RequestPause();
            }
        }

        if (@event.IsActionPressed(InputActions.Interact))
        {
            TryPlaceBlock();
        }
    }

    /// <inheritdoc />
    public override void _PhysicsProcess(double delta)
    {
        if (_playerData is null)
        {
            return;
        }

        float deltaTime = (float)delta;

        PlayerMovementSettings settings = _playerData.MovementSettings;

        if (!IsOnFloor())
        {
            Velocity = new Vector3(Velocity.X,
                Velocity.Y - settings.Gravity * deltaTime,
                Velocity.Z);
        }

        if (Input.IsActionJustPressed(InputActions.Jump) && IsOnFloor())
        {
            Velocity = new Vector3(Velocity.X, settings.JumpVelocity, Velocity.Z);
        }

        _playerData.IsSprinting = Input.IsActionPressed(InputActions.Sprint);
        float speed = _playerData.IsSprinting ? settings.SprintSpeed : settings.WalkSpeed;

        Vector2 inputDirection = Vector2.Zero;

        if (Input.IsActionPressed(InputActions.MoveForward))
        {
            inputDirection.Y -= 1;
        }

        if (Input.IsActionPressed(InputActions.MoveBack))
        {
            inputDirection.Y += 1;
        }

        if (Input.IsActionPressed(InputActions.MoveLeft))
        {
            inputDirection.X -= 1;
        }

        if (Input.IsActionPressed(InputActions.MoveRight))
        {
            inputDirection.X += 1;
        }

        inputDirection = inputDirection.Normalized();

        float yaw = _playerData.CameraYaw;
        Vector3 forward = new(-MathF.Sin(yaw), 0, -MathF.Cos(yaw));
        Vector3 right = new(MathF.Cos(yaw), 0, -MathF.Sin(yaw));
        Vector3 moveDirection = forward * -inputDirection.Y + right * inputDirection.X;

        Velocity = new Vector3(
            moveDirection.X * speed,
            Velocity.Y,
            moveDirection.Z * speed);

        MoveAndSlide();

        _playerData.PositionX = Position.X;
        _playerData.PositionY = Position.Y;
        _playerData.PositionZ = Position.Z;

        PublishPositionIfMoved();
        TickMiningInput(deltaTime);
    }

    private void OnWorldReady(WorldReadyEvent evt)
    {
        _eventBus.Unsubscribe<WorldReadyEvent>(OnWorldReady);
        ProcessMode = ProcessModeEnum.Inherit;
        CaptureMouse();

        // Seed the position tracking so PublishPositionIfMoved sends on the first frame
        _lastPublishedX = Position.X;
        _lastPublishedY = Position.Y;
        _lastPublishedZ = Position.Z;

        // Force-publish the initial position so WorldNode triggers
        // PlayerChunkChangedEvent and ChunkLoadingScheduler starts the full render distance.
        _eventBus.Publish(new PlayerPositionUpdatedEvent
        {
            X = Position.X,
            Y = Position.Y,
            Z = Position.Z,
        });

        _logger.Info("PlayerNode: WorldReady — gameplay started at {0}.", Position);
    }

    private void PublishPositionIfMoved()
    {
        float deltaX = Position.X - _lastPublishedX;
        float deltaY = Position.Y - _lastPublishedY;
        float deltaZ = Position.Z - _lastPublishedZ;

        if (deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ <= PositionPublishThresholdSquared)
        {
            return;
        }

        _lastPublishedX = Position.X;
        _lastPublishedY = Position.Y;
        _lastPublishedZ = Position.Z;

        _eventBus.Publish(new PlayerPositionUpdatedEvent
        {
            X = Position.X,
            Y = Position.Y,
            Z = Position.Z,
        });
    }

    private void TickMiningInput(float deltaTime)
    {
        if (!ResolveBlockInteraction())
        {
            return;
        }

        if (Input.IsActionPressed(InputActions.Attack))
        {
            Vector3 origin = _camera.GlobalPosition;
            Vector3 forward = -_camera.GlobalTransform.Basis.Z;
            float range = _playerData.MovementSettings.InteractionRange;

            _blockInteraction!.TickMining(
                origin.X, origin.Y, origin.Z,
                forward.X, forward.Y, forward.Z,
                range, deltaTime);
        }
        else
        {
            _blockInteraction!.CancelMining();
        }
    }

    private void TryPlaceBlock()
    {
        if (!ResolveBlockInteraction())
        {
            return;
        }

        Vector3 origin = _camera.GlobalPosition;
        Vector3 forward = -_camera.GlobalTransform.Basis.Z;
        float range = _playerData.MovementSettings.InteractionRange;

        _blockInteraction!.TryPlaceBlock(
            origin.X, origin.Y, origin.Z,
            forward.X, forward.Y, forward.Z,
            range, _playerData.SelectedBlockId);
    }

    /// <summary>
    /// Lazily resolves IBlockInteractionService from the ServiceLocator.
    /// Needed because GameBootstrapper registers it via CallDeferred,
    /// which runs after all nodes have had their _Ready() called.
    /// </summary>
    private bool ResolveBlockInteraction()
    {
        if (_blockInteraction is not null)
        {
            return true;
        }

        if (ServiceLocator.Instance.TryGet<IBlockInteractionService>(out IBlockInteractionService? service))
        {
            _blockInteraction = service;
            _logger.Info("PlayerNode: IBlockInteractionService resolved (lazy).");
            return true;
        }

        return false;
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
