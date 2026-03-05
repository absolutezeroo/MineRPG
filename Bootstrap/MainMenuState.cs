using MineRPG.Core.StateMachine;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Game state representing the main menu. Scene transition is handled by
/// <see cref="GameStateOrchestrator"/>; this state is a lifecycle sentinel.
/// </summary>
public sealed class MainMenuState : IState
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
