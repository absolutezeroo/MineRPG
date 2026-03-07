namespace MineRPG.Core.Events.Definitions;

/// <summary>
/// Published when the player presses the pause action during active gameplay.
/// <c>GameStateOrchestrator</c> subscribes and delegates to
/// <see cref="MineRPG.Core.Interfaces.Lifecycle.IGameStateController.RequestPause"/>.
/// </summary>
public readonly struct PauseRequestedEvent
{
}
