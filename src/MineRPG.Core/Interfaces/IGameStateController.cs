namespace MineRPG.Core.Interfaces;

/// <summary>
/// Controls game state transitions (pause/resume).
/// Implemented by the game orchestrator, consumed by UI nodes.
/// This interface lives in Core so Godot.UI can reference it without
/// depending on the Bootstrap layer.
/// </summary>
public interface IGameStateController
{
    /// <summary>
    /// Requests the game to pause.
    /// </summary>
    void RequestPause();

    /// <summary>
    /// Requests the game to resume from pause.
    /// </summary>
    void RequestResume();
}
