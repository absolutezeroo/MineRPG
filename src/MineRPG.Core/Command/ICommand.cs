namespace MineRPG.Core.Command;

/// <summary>
/// Encapsulates a reversible player action.
/// Used for input remapping, undo, and replay recording.
/// </summary>
public interface ICommand
{
    bool CanExecute();
    void Execute();
    bool CanUndo { get; }
    void Undo();
}
