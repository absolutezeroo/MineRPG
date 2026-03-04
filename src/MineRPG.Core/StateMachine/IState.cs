namespace MineRPG.Core.StateMachine;

/// <summary>
/// A single state in a pushdown automaton.
/// </summary>
public interface IState
{
    /// <summary>
    /// Called when the state becomes the active top-of-stack.
    /// </summary>
    void Enter();

    /// <summary>
    /// Called when the state is removed from the stack.
    /// </summary>
    void Exit();

    /// <summary>
    /// Called every tick while this state is the active top-of-stack.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    void Tick(float deltaTime);

    /// <summary>
    /// Called when a new state is pushed on top of this one.
    /// Override to suppress music, animations, etc.
    /// </summary>
    void Pause()
    {
    }

    /// <summary>
    /// Called when the state above this one is popped,
    /// restoring this state as the active top-of-stack.
    /// </summary>
    void Resume()
    {
    }
}
