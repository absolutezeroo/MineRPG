using MineRPG.Core.Interfaces;
using MineRPG.Entities.Player;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Maps hotbar slot selection to PlayerData.SelectedBlockId.
/// Registered in <see cref="CompositionRoot"/> as <see cref="IHotbarController"/>.
/// </summary>
public sealed class HotbarController(PlayerData playerData) : IHotbarController
{
    /// <summary>
    /// Gets the currently selected hotbar slot index.
    /// </summary>
    public int SelectedIndex { get; private set; }

    /// <summary>
    /// Selects the hotbar slot at the given index and updates the player's selected block.
    /// </summary>
    /// <param name="index">The hotbar slot index to select.</param>
    public void SelectSlot(int index)
    {
        SelectedIndex = index;
        playerData.SelectedBlockId = (ushort)(index + 1);
    }
}
