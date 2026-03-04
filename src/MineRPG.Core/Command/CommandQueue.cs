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
public sealed class CommandQueue(ILogger logger, int maxUndoDepth = CommandQueue.DefaultMaxUndoDepth)
{
    public const int DefaultMaxUndoDepth = 100;

    private readonly Queue<ICommand> _pending = new();
    private readonly Stack<ICommand> _undoStack = new();

    public int PendingCount => _pending.Count;
    public int UndoCount => _undoStack.Count;

    public void Enqueue(ICommand command) => _pending.Enqueue(command);

    /// <summary>
    /// Execute all pending commands in FIFO order.
    /// Commands where CanExecute() returns false are silently discarded.
    /// </summary>
    public void Process()
    {
        while (_pending.TryDequeue(out var command))
        {
            if (!command.CanExecute())
            {
                logger.Debug("CommandQueue: Skipped {0} (CanExecute = false)", command.GetType().Name);
                continue;
            }

            try
            {
                command.Execute();
                logger.Debug("CommandQueue: Executed {0}", command.GetType().Name);

                if (command.CanUndo)
                {
                    if (_undoStack.Count >= maxUndoDepth)
                        TrimUndoStack();

                    _undoStack.Push(command);
                }
            }
            catch (Exception ex)
            {
                logger.Error("CommandQueue: {0} threw during Execute", ex, command.GetType().Name);
            }
        }
    }

    /// <summary>
    /// Undo the most recently executed undoable command.
    /// Returns true if an undo was performed.
    /// </summary>
    public bool Undo()
    {
        if (!_undoStack.TryPop(out var command))
            return false;

        try
        {
            command.Undo();
            logger.Debug("CommandQueue: Undid {0}", command.GetType().Name);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error("CommandQueue: {0} threw during Undo", ex, command.GetType().Name);
            return false;
        }
    }

    public void Clear()
    {
        _pending.Clear();
        _undoStack.Clear();
    }

    // Trim the bottom half of the undo stack when it hits capacity
    private void TrimUndoStack()
    {
        var items = _undoStack.ToArray();
        _undoStack.Clear();
        var keep = maxUndoDepth / 2;
        for (var i = keep - 1; i >= 0; i--)
            _undoStack.Push(items[i]);
    }
}
