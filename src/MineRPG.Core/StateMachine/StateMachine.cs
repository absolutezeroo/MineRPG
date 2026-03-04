using System;
using System.Collections.Generic;

using MineRPG.Core.Logging;

namespace MineRPG.Core.StateMachine;

/// <summary>
/// Pushdown automaton state machine that supports nested states.
/// </summary>
public sealed class StateMachine : IStateMachine
{
    private readonly Stack<IState> _stack = new();
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="StateMachine"/>.
    /// </summary>
    /// <param name="logger">Logger for state transition diagnostics.</param>
    public StateMachine(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IState? CurrentState => _stack.TryPeek(out IState? top) ? top : null;

    /// <inheritdoc />
    public int Depth => _stack.Count;

    /// <inheritdoc />
    public void ChangeState(IState state)
    {
        if (_stack.TryPop(out IState? current))
        {
            _logger.Debug("StateMachine: Exit {0}", current.GetType().Name);
            current.Exit();
        }

        _logger.Debug("StateMachine: Enter {0}", state.GetType().Name);
        _stack.Push(state);
        state.Enter();
    }

    /// <inheritdoc />
    public void PushState(IState state)
    {
        if (_stack.TryPeek(out IState? current))
        {
            _logger.Debug("StateMachine: Pause {0}", current.GetType().Name);
            current.Pause();
        }

        _logger.Debug("StateMachine: Enter (push) {0}", state.GetType().Name);
        _stack.Push(state);
        state.Enter();
    }

    /// <inheritdoc />
    public void PopState()
    {
        if (_stack.Count == 0)
        {
            throw new InvalidOperationException("Cannot pop state: the state machine stack is empty.");
        }

        IState popped = _stack.Pop();
        _logger.Debug("StateMachine: Exit (pop) {0}", popped.GetType().Name);
        popped.Exit();

        if (_stack.TryPeek(out IState? resumed))
        {
            _logger.Debug("StateMachine: Resume {0}", resumed.GetType().Name);
            resumed.Resume();
        }
    }

    /// <inheritdoc />
    public void Tick(float deltaTime)
    {
        if (_stack.TryPeek(out IState? top))
        {
            top.Tick(deltaTime);
        }
    }

    /// <inheritdoc />
    public void TickAll(float deltaTime)
    {
        // Stack enumerates top-to-bottom; copy to array and iterate bottom-to-top
        // to avoid LINQ Reverse() allocation in this potentially per-frame method.
        IState[] states = _stack.ToArray();

        for (int i = states.Length - 1; i >= 0; i--)
        {
            states[i].Tick(deltaTime);
        }
    }
}
