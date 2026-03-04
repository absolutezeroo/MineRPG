using MineRPG.Core.Interfaces;
using MineRPG.Entities.Player;

namespace MineRPG.Game.Bootstrap;

/// <summary>
/// Maps hotbar slot selection to PlayerData.SelectedBlockId.
/// Registered in <see cref="CompositionRoot"/> as <see cref="IHotbarController"/>.
/// </summary>
public sealed class HotbarController(PlayerData playerData) : IHotbarController
{
    public int SelectedIndex { get; private set; }

    public void SelectSlot(int index)
    {
        SelectedIndex = index;
        playerData.SelectedBlockId = (ushort)(index + 1);
    }
}
