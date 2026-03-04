namespace MineRPG.Core.Interfaces;

/// <summary>
/// Controls the selected hotbar slot and block ID.
/// Implemented by the Game layer, consumed by Godot.UI.
/// Decouples UI from the Entities project.
/// </summary>
public interface IHotbarController
{
    int SelectedIndex { get; }
    void SelectSlot(int index);
}
