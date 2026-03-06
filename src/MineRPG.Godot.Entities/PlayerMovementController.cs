using System;

using Godot;

using MineRPG.Core.Logging;
using MineRPG.Entities.Player;

namespace MineRPG.Godot.Entities;

/// <summary>
/// Handles walk and fly movement for the player character.
/// Updates velocity on the owning CharacterBody3D each physics frame.
/// </summary>
internal sealed class PlayerMovementController
{
    /// <summary>Floor snap length for walking mode. Disabled in fly mode.</summary>
    private const float DefaultFloorSnapLength = 0.1f;

    private readonly PlayerData _playerData;
    private readonly ILogger _logger;

    /// <summary>
    /// Creates a movement controller for the given player data.
    /// </summary>
    /// <param name="playerData">Player data containing movement settings and state.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public PlayerMovementController(PlayerData playerData, ILogger logger)
    {
        _playerData = playerData;
        _logger = logger;
    }

    /// <summary>
    /// Processes walk movement: gravity, jump, sprint, and horizontal direction.
    /// </summary>
    /// <param name="body">The CharacterBody3D to move.</param>
    /// <param name="deltaTime">Physics frame delta in seconds.</param>
    public void ProcessWalkMovement(CharacterBody3D body, float deltaTime)
    {
        PlayerMovementSettings settings = _playerData.MovementSettings;

        if (!body.IsOnFloor())
        {
            body.Velocity = new Vector3(body.Velocity.X,
                body.Velocity.Y - settings.Gravity * deltaTime,
                body.Velocity.Z);
        }

        if (Input.IsActionJustPressed(InputActions.Jump) && body.IsOnFloor())
        {
            body.Velocity = new Vector3(body.Velocity.X, settings.JumpVelocity, body.Velocity.Z);
        }

        _playerData.IsSprinting = Input.IsActionPressed(InputActions.Sprint);
        float speed = _playerData.IsSprinting ? settings.SprintSpeed : settings.WalkSpeed;

        Vector2 inputDirection = ReadHorizontalInput();

        float yaw = _playerData.CameraYaw;
        Vector3 forward = new(-MathF.Sin(yaw), 0, -MathF.Cos(yaw));
        Vector3 right = new(MathF.Cos(yaw), 0, -MathF.Sin(yaw));
        Vector3 moveDirection = forward * -inputDirection.Y + right * inputDirection.X;

        body.Velocity = new Vector3(
            moveDirection.X * speed,
            body.Velocity.Y,
            moveDirection.Z * speed);
    }

    /// <summary>
    /// Processes fly movement: horizontal direction plus vertical via jump/sprint.
    /// </summary>
    /// <param name="body">The CharacterBody3D to move.</param>
    public void ProcessFlyMovement(CharacterBody3D body)
    {
        float speed = _playerData.CurrentFlySpeed;
        Vector2 inputDirection = ReadHorizontalInput();

        float yaw = _playerData.CameraYaw;
        Vector3 forward = new(-MathF.Sin(yaw), 0, -MathF.Cos(yaw));
        Vector3 right = new(MathF.Cos(yaw), 0, -MathF.Sin(yaw));
        Vector3 moveDirection = forward * -inputDirection.Y + right * inputDirection.X;

        float verticalSpeed = 0f;

        if (Input.IsActionPressed(InputActions.Jump))
        {
            verticalSpeed = speed;
        }

        if (Input.IsActionPressed(InputActions.Sprint))
        {
            verticalSpeed = -speed;
        }

        body.Velocity = new Vector3(
            moveDirection.X * speed,
            verticalSpeed,
            moveDirection.Z * speed);
    }

    /// <summary>
    /// Handles fly toggle and fly speed adjustment input.
    /// </summary>
    /// <param name="event">The input event to process.</param>
    /// <param name="body">The CharacterBody3D for floor snap adjustment.</param>
    public void HandleFlyInput(InputEvent @event, CharacterBody3D body)
    {
        if (@event.IsActionPressed(InputActions.ToggleFly))
        {
            _playerData.IsFlying = !_playerData.IsFlying;
            body.FloorSnapLength = _playerData.IsFlying ? 0f : DefaultFloorSnapLength;

            if (_playerData.IsFlying)
            {
                body.Velocity = new Vector3(body.Velocity.X, 0f, body.Velocity.Z);
            }

            _logger.Info("Fly mode {0} (speed: {1})",
                _playerData.IsFlying ? "ON" : "OFF",
                _playerData.CurrentFlySpeed);
            return;
        }

        if (!_playerData.IsFlying)
        {
            return;
        }

        if (@event.IsActionPressed(InputActions.FlySpeedUp))
        {
            PlayerMovementSettings settings = _playerData.MovementSettings;
            _playerData.CurrentFlySpeed = MathF.Min(
                _playerData.CurrentFlySpeed + settings.FlySpeedStep,
                settings.MaxFlySpeed);
            _logger.Info("Fly speed: {0}", _playerData.CurrentFlySpeed);
        }
        else if (@event.IsActionPressed(InputActions.FlySpeedDown))
        {
            PlayerMovementSettings settings = _playerData.MovementSettings;
            _playerData.CurrentFlySpeed = MathF.Max(
                _playerData.CurrentFlySpeed - settings.FlySpeedStep,
                settings.MinFlySpeed);
            _logger.Info("Fly speed: {0}", _playerData.CurrentFlySpeed);
        }
    }

    private static Vector2 ReadHorizontalInput()
    {
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

        return inputDirection.Normalized();
    }
}
