namespace MineRPG.Core.StateMachine;

/// <summary>
/// A single state in a pushdown automaton.
/// </summary>
public interface IState
{
    void Enter();
    void Exit();
    void Tick(float deltaTime);

    /// <summary>
    /// Called when a new state is pushed on top of this one.
    /// Override to suppress music, animations, etc.
    /// </summary>
    void Pause() { }

    /// <summary>
    /// Called when the state above this one is popped,
    /// restoring this state as the active top-of-stack.
    /// </summary>
    void Resume() { }
}
