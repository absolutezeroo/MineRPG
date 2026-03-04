namespace MineRPG.Core.StateMachine;

/// <summary>
/// Pushdown automaton. Supports nested states: e.g., player in combat
/// while inside a dialogue — push dialogue, pop back to combat.
/// </summary>
public interface IStateMachine
{
    IState? CurrentState { get; }
    int Depth { get; }

    /// <summary>
    /// Replace the current top state. Calls Exit on old, Enter on new.
    /// </summary>
    void ChangeState(IState state);

    /// <summary>
    /// Push a new state on top. Calls Pause on current, Enter on new.
    /// </summary>
    void PushState(IState state);

    /// <summary>
    /// Pop the top state. Calls Exit on current, Resume on new top (if any).
    /// </summary>
    void PopState();

    void Tick(float deltaTime);

    /// <summary>
    /// Tick every state in the stack from bottom to top.
    /// Use when background states need updates (e.g., buff timers while in dialogue).
    /// </summary>
    void TickAll(float deltaTime);
}
