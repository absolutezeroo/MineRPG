using Godot;
using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Interfaces;
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
	[Export] private Camera3D _camera = null!;

	private PlayerData _playerData = null!;
	private IEventBus _eventBus = null!;
	private ILogger _logger = null!;
	private IBlockInteractionService? _blockInteraction;

	private bool _mouseCaptured;
	private float _lastPublishedX;
	private float _lastPublishedY;
	private float _lastPublishedZ;

	public override void _Ready()
	{
		_playerData = ServiceLocator.Instance.Get<PlayerData>();
		_eventBus = ServiceLocator.Instance.Get<IEventBus>();
		_logger = ServiceLocator.Instance.Get<ILogger>();

		// Resolve camera — [Export] NodePath may not auto-resolve on private fields
		_camera ??= GetNode<Camera3D>("Camera3D");

		if (ServiceLocator.Instance.TryGet<IBlockInteractionService>(out var blockInteraction))
			_blockInteraction = blockInteraction;

		CaptureMouse();
		_logger.Info("PlayerNode ready at {0}, camera={1}", Position, _camera?.Name ?? "null");
	}

	public override void _Input(InputEvent @event)
	{
		if (_playerData is null || _camera is null)
			return;

		if (@event is InputEventMouseMotion mouseMotion && _mouseCaptured)
		{
			var sens = _playerData.MovementSettings.MouseSensitivity;
			_playerData.CameraYaw -= mouseMotion.Relative.X * sens;
			_playerData.CameraPitch -= mouseMotion.Relative.Y * sens;
			_playerData.CameraPitch = Mathf.Clamp(_playerData.CameraPitch,
				Mathf.DegToRad(-89f), Mathf.DegToRad(89f));

			Rotation = new Vector3(0, _playerData.CameraYaw, 0);
			_camera.Rotation = new Vector3(_playerData.CameraPitch, 0, 0);
		}

		if (@event.IsActionPressed(InputActions.Pause))
		{
			if (_mouseCaptured) ReleaseMouse();
			else CaptureMouse();
		}

		if (@event.IsActionPressed(InputActions.Attack))
			TryBreakBlock();

		if (@event.IsActionPressed(InputActions.Interact))
			TryPlaceBlock();
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_playerData is null)
			return;

		var dt = (float)delta;
		var settings = _playerData.MovementSettings;

		if (!IsOnFloor())
		{
			Velocity = new Vector3(Velocity.X,
				Velocity.Y - settings.Gravity * dt,
				Velocity.Z);
		}

		if (Input.IsActionJustPressed(InputActions.Jump) && IsOnFloor())
		{
			Velocity = new Vector3(Velocity.X, settings.JumpVelocity, Velocity.Z);
		}

		_playerData.IsSprinting = Input.IsActionPressed(InputActions.Sprint);
		var speed = _playerData.IsSprinting ? settings.SprintSpeed : settings.WalkSpeed;

		var inputDir = Vector2.Zero;
		if (Input.IsActionPressed(InputActions.MoveForward)) inputDir.Y -= 1;
		if (Input.IsActionPressed(InputActions.MoveBack)) inputDir.Y += 1;
		if (Input.IsActionPressed(InputActions.MoveLeft)) inputDir.X -= 1;
		if (Input.IsActionPressed(InputActions.MoveRight)) inputDir.X += 1;
		inputDir = inputDir.Normalized();

		var yaw = _playerData.CameraYaw;
		var forward = new Vector3(-MathF.Sin(yaw), 0, -MathF.Cos(yaw));
		var right = new Vector3(MathF.Cos(yaw), 0, -MathF.Sin(yaw));
		var moveDir = forward * -inputDir.Y + right * inputDir.X;

		Velocity = new Vector3(
			moveDir.X * speed,
			Velocity.Y,
			moveDir.Z * speed);

		MoveAndSlide();

		_playerData.PositionX = Position.X;
		_playerData.PositionY = Position.Y;
		_playerData.PositionZ = Position.Z;

		// Only publish when the player has actually moved (dead zone 0.01 units)
		var dxp = Position.X - _lastPublishedX;
		var dyp = Position.Y - _lastPublishedY;
		var dzp = Position.Z - _lastPublishedZ;
		if (dxp * dxp + dyp * dyp + dzp * dzp > 0.0001f)
		{
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
	}

	private void TryBreakBlock()
	{
		if (_blockInteraction is null)
			return;

		var origin = _camera.GlobalPosition;
		var forward = -_camera.GlobalTransform.Basis.Z;
		var range = _playerData.MovementSettings.InteractionRange;

		_blockInteraction.TryBreakBlock(
			origin.X, origin.Y, origin.Z,
			forward.X, forward.Y, forward.Z,
			range);
	}

	private void TryPlaceBlock()
	{
		if (_blockInteraction is null)
			return;

		var origin = _camera.GlobalPosition;
		var forward = -_camera.GlobalTransform.Basis.Z;
		var range = _playerData.MovementSettings.InteractionRange;

		_blockInteraction.TryPlaceBlock(
			origin.X, origin.Y, origin.Z,
			forward.X, forward.Y, forward.Z,
			range, _playerData.SelectedBlockId);
	}

	private void CaptureMouse()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		_mouseCaptured = true;
	}

	private void ReleaseMouse()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		_mouseCaptured = false;
	}
}
