namespace MineRPG.Core.Command;

/// <summary>
/// Encapsulates a reversible player action.
/// Used for input remapping, undo, and replay recording.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Whether this command is currently valid and can be executed.
    /// </summary>
    /// <returns>True if the command can be executed in the current state.</returns>
    bool CanExecute();

    /// <summary>
    /// Execute the command, applying its effects.
    /// </summary>
    void Execute();

    /// <summary>
    /// Whether this command supports being undone.
    /// </summary>
    bool CanUndo { get; }

    /// <summary>
    /// Reverse the effects of a previously executed command.
    /// </summary>
    void Undo();
}
