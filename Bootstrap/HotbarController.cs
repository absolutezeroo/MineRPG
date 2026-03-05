using MineRPG.Core.Interfaces;
using MineRPG.Entities.Player;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Maps hotbar slot selection to PlayerData.SelectedBlockId.
/// Registered in <see cref="CompositionRoot"/> as <see cref="IHotbarController"/>.
/// </summary>
public sealed class HotbarController(PlayerData playerData) : IHotbarController
{
    private const int MinSlotIndex = 0;
    private const int MaxSlotIndex = 8;

    /// <summary>
    /// Gets the currently selected hotbar slot index.
    /// </summary>
    public int SelectedIndex { get; private set; }

    /// <summary>
    /// Selects the hotbar slot at the given index and updates the player's selected block.
    /// </summary>
    /// <param name="index">The hotbar slot index to select (0-8).</param>
    public void SelectSlot(int index)
    {
        int clamped = Math.Clamp(index, MinSlotIndex, MaxSlotIndex);
        SelectedIndex = clamped;
        playerData.SelectedBlockId = (ushort)(clamped + 1);
    }
}
