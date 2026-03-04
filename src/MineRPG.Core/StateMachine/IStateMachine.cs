namespace MineRPG.Core.StateMachine;

/// <summary>
/// Pushdown automaton. Supports nested states: e.g., player in combat
/// while inside a dialogue — push dialogue, pop back to combat.
/// </summary>
public interface IStateMachine
{
    /// <summary>
    /// The currently active (top-of-stack) state, or null if the stack is empty.
    /// </summary>
    IState? CurrentState { get; }

    /// <summary>
    /// The number of states currently on the stack.
    /// </summary>
    int Depth { get; }

    /// <summary>
    /// Replace the current top state. Calls Exit on old, Enter on new.
    /// </summary>
    /// <param name="state">The new state to transition to.</param>
    void ChangeState(IState state);

    /// <summary>
    /// Push a new state on top. Calls Pause on current, Enter on new.
    /// </summary>
    /// <param name="state">The state to push onto the stack.</param>
    void PushState(IState state);

    /// <summary>
    /// Pop the top state. Calls Exit on current, Resume on new top (if any).
    /// </summary>
    void PopState();

    /// <summary>
    /// Tick only the top state.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    void Tick(float deltaTime);

    /// <summary>
    /// Tick every state in the stack from bottom to top.
    /// Use when background states need updates (e.g., buff timers while in dialogue).
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    void TickAll(float deltaTime);
}
