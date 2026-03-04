namespace MineRPG.Core.Interfaces;

/// <summary>
/// Controls the selected hotbar slot and block ID.
/// Implemented by the Game layer, consumed by Godot.UI.
/// Decouples UI from the Entities project.
/// </summary>
public interface IHotbarController
{
    /// <summary>
    /// The zero-based index of the currently selected hotbar slot.
    /// </summary>
    public int SelectedIndex { get; }

    /// <summary>
    /// Select a hotbar slot by its zero-based index.
    /// </summary>
    /// <param name="index">The slot index to select.</param>
    public void SelectSlot(int index);
}
