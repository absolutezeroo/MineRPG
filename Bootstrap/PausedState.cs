using MineRPG.Core.StateMachine;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Pushed on top of <see cref="PlayingState"/> when the game is paused.
/// The actual tree pausing is handled by <see cref="PlayingState.Pause"/> and
/// <see cref="PlayingState.Resume"/>. This state is a stack sentinel.
/// </summary>
public sealed class PausedState : IState
{
    /// <inheritdoc />
    public void Enter()
    {
    }

    /// <inheritdoc />
    public void Exit()
    {
    }

    /// <inheritdoc />
    public void Tick(float deltaTime)
    {
    }
}
