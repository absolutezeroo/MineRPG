using MineRPG.Core.Logging;

namespace MineRPG.Core.StateMachine;

public sealed class StateMachine(ILogger logger) : IStateMachine
{
    private readonly Stack<IState> _stack = new();

    public IState? CurrentState => _stack.TryPeek(out var top) ? top : null;
    public int Depth => _stack.Count;

    public void ChangeState(IState state)
    {
        if (_stack.TryPop(out var current))
        {
            logger.Debug("StateMachine: Exit {0}", current.GetType().Name);
            current.Exit();
        }

        logger.Debug("StateMachine: Enter {0}", state.GetType().Name);
        _stack.Push(state);
        state.Enter();
    }

    public void PushState(IState state)
    {
        if (_stack.TryPeek(out var current))
        {
            logger.Debug("StateMachine: Pause {0}", current.GetType().Name);
            current.Pause();
        }

        logger.Debug("StateMachine: Enter (push) {0}", state.GetType().Name);
        _stack.Push(state);
        state.Enter();
    }

    public void PopState()
    {
        if (_stack.Count == 0)
            throw new InvalidOperationException("Cannot pop state: the state machine stack is empty.");

        var popped = _stack.Pop();
        logger.Debug("StateMachine: Exit (pop) {0}", popped.GetType().Name);
        popped.Exit();

        if (_stack.TryPeek(out var resumed))
        {
            logger.Debug("StateMachine: Resume {0}", resumed.GetType().Name);
            resumed.Resume();
        }
    }

    public void Tick(float deltaTime)
    {
        if (_stack.TryPeek(out var top))
            top.Tick(deltaTime);
    }

    public void TickAll(float deltaTime)
    {
        // Stack enumerates top-to-bottom; reverse for bottom-to-top order.
        foreach (var state in _stack.Reverse())
            state.Tick(deltaTime);
    }
}
