namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published by the death screen UI when the player clicks the Respawn button.
/// The survival system listens for this to reset vitals and teleport to spawn.
/// </summary>
public readonly struct RespawnRequestedEvent
{
}
