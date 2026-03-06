#if DEBUG
namespace MineRPG.Godot.UI.Debug.Tabs;

/// <summary>
/// Interface for debug menu tab panels. Each tab updates its display
/// when active and visible.
/// </summary>
public interface IDebugTab
{
    /// <summary>
    /// Updates the tab's display. Called each frame by DebugMenuPanel
    /// when this tab is active.
    /// </summary>
    /// <param name="delta">Frame delta time in seconds.</param>
    void UpdateDisplay(double delta);
}
#endif
