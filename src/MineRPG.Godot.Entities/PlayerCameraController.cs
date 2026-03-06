using System;

using Godot;

using MineRPG.Entities.Player;

namespace MineRPG.Godot.Entities;

/// <summary>
/// Handles mouse-look camera rotation for the player.
/// Clamps pitch to prevent full vertical rotation.
/// </summary>
internal sealed class PlayerCameraController
{
    private const float MinPitchRadians = -89f * MathF.PI / 180f;
    private const float MaxPitchRadians = 89f * MathF.PI / 180f;

    private readonly PlayerData _playerData;
    private readonly Camera3D _camera;

    /// <summary>
    /// Creates a camera controller for the given player data and camera node.
    /// </summary>
    /// <param name="playerData">Player data containing yaw, pitch, and sensitivity.</param>
    /// <param name="camera">The Godot Camera3D node to rotate.</param>
    public PlayerCameraController(PlayerData playerData, Camera3D camera)
    {
        _playerData = playerData;
        _camera = camera;
    }

    /// <summary>
    /// Processes mouse motion input, updating yaw and pitch on the player data
    /// and applying the rotation to the owning node and camera.
    /// </summary>
    /// <param name="mouseMotion">The mouse motion event.</param>
    /// <param name="ownerNode">The CharacterBody3D node to rotate horizontally.</param>
    public void HandleMouseMotion(InputEventMouseMotion mouseMotion, Node3D ownerNode)
    {
        float sensitivity = _playerData.MovementSettings.MouseSensitivity;
        _playerData.CameraYaw -= mouseMotion.Relative.X * sensitivity;
        _playerData.CameraPitch -= mouseMotion.Relative.Y * sensitivity;
        _playerData.CameraPitch = Mathf.Clamp(_playerData.CameraPitch, MinPitchRadians, MaxPitchRadians);

        ownerNode.Rotation = new Vector3(0, _playerData.CameraYaw, 0);
        _camera.Rotation = new Vector3(_playerData.CameraPitch, 0, 0);
    }
}
