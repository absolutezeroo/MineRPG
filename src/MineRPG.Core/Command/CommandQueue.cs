using System;
using System.Collections.Generic;

using MineRPG.Core.Logging;

namespace MineRPG.Core.Command;

/// <summary>
/// Queues commands for deferred execution and maintains an undo stack.
///
/// Usage:
///   1. Systems enqueue commands (e.g., PlaceBlockCommand, MineBlockCommand).
///   2. The game loop calls Process() once per tick.
///   3. UI calls Undo() when the player presses Ctrl+Z.
/// </summary>
public sealed class CommandQueue
{
    /// <summary>
    /// Default maximum number of undoable commands retained.
    /// </summary>
    public const int DefaultMaxUndoDepth = 100;

    private readonly Queue<ICommand> _pending = new();
    private readonly Stack<ICommand> _undoStack = new();
    private readonly ILogger _logger;
    private readonly int _maxUndoDepth;

    /// <summary>
    /// Initializes a new instance of <see cref="CommandQueue"/>.
    /// </summary>
    /// <param name="logger">Logger for command execution diagnostics.</param>
    /// <param name="maxUndoDepth">Maximum number of undoable commands to retain.</param>
    public CommandQueue(ILogger logger, int maxUndoDepth = DefaultMaxUndoDepth)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxUndoDepth = maxUndoDepth;
    }

    /// <summary>
    /// Number of commands waiting to be executed.
    /// </summary>
    public int PendingCount => _pending.Count;

    /// <summary>
    /// Number of undoable commands on the undo stack.
    /// </summary>
    public int UndoCount => _undoStack.Count;

    /// <summary>
    /// Enqueue a command for deferred execution.
    /// </summary>
    /// <param name="command">The command to enqueue.</param>
    public void Enqueue(ICommand command) => _pending.Enqueue(command);

    /// <summary>
    /// Execute all pending commands in FIFO order.
    /// Commands where CanExecute() returns false are silently discarded.
    /// </summary>
    public void Process()
    {
        while (_pending.TryDequeue(out ICommand? command))
        {
            if (!command.CanExecute())
            {
                _logger.Debug("CommandQueue: Skipped {0} (CanExecute = false)", command.GetType().Name);
                continue;
            }

            try
            {
                command.Execute();
                _logger.Debug("CommandQueue: Executed {0}", command.GetType().Name);

                if (command.CanUndo)
                {
                    if (_undoStack.Count >= _maxUndoDepth)
                    {
                        TrimUndoStack();
                    }

                    _undoStack.Push(command);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("CommandQueue: {0} threw during Execute", ex, command.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Undo the most recently executed undoable command.
    /// </summary>
    /// <returns>True if an undo was performed; false if the undo stack was empty.</returns>
    public bool Undo()
    {
        if (!_undoStack.TryPop(out ICommand? command))
        {
            return false;
        }

        try
        {
            command.Undo();
            _logger.Debug("CommandQueue: Undid {0}", command.GetType().Name);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error("CommandQueue: {0} threw during Undo", ex, command.GetType().Name);
            return false;
        }
    }

    /// <summary>
    /// Clear all pending commands and the undo stack.
    /// </summary>
    public void Clear()
    {
        _pending.Clear();
        _undoStack.Clear();
    }

    // Trim the bottom half of the undo stack when it hits capacity
    private void TrimUndoStack()
    {
        ICommand[] items = _undoStack.ToArray();
        _undoStack.Clear();
        int keep = _maxUndoDepth / 2;

        for (int i = keep - 1; i >= 0; i--)
        {
            _undoStack.Push(items[i]);
        }
    }
}
