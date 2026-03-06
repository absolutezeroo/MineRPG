using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Entities.Player;
using MineRPG.RPG.Items;

namespace MineRPG.Game.Bootstrap.Gameplay;

/// <summary>
/// Maps hotbar slot selection to PlayerData and provides access to the currently
/// held item and its tool properties. Registered in <see cref="CompositionRoot"/>
/// as <see cref="IHotbarController"/>.
/// </summary>
public sealed class HotbarController : IHotbarController
{
    private const int MinSlotIndex = 0;
    private const int MaxSlotIndex = 8;

    private readonly PlayerData _playerData;
    private readonly ItemRegistry _itemRegistry;

    /// <summary>
    /// Creates a hotbar controller linked to the player data and item registry.
    /// </summary>
    /// <param name="playerData">Player state container.</param>
    /// <param name="itemRegistry">Item registry for definition lookups.</param>
    public HotbarController(PlayerData playerData, ItemRegistry itemRegistry)
    {
        _playerData = playerData;
        _itemRegistry = itemRegistry;
    }

    /// <summary>
    /// Gets the currently selected hotbar slot index.
    /// </summary>
    public int SelectedIndex { get; private set; }

    /// <summary>
    /// Selects the hotbar slot at the given index and updates the player's state.
    /// </summary>
    /// <param name="index">The hotbar slot index to select (0-8).</param>
    public void SelectSlot(int index)
    {
        int clamped = Math.Clamp(index, MinSlotIndex, MaxSlotIndex);
        SelectedIndex = clamped;
        _playerData.SelectedHotbarSlot = clamped;
        _playerData.SelectedBlockId = (ushort)(clamped + 1);
    }

    /// <summary>
    /// Returns the <see cref="ItemInstance"/> currently held in the selected hotbar slot,
    /// or null if the slot is empty or no inventory is connected.
    /// </summary>
    /// <returns>The held item, or null.</returns>
    public ItemInstance? GetSelectedItem()
    {
        if (_playerData.Inventory is null)
        {
            return null;
        }

        return _playerData.Inventory.Hotbar.GetSlot(_playerData.SelectedHotbarSlot);
    }

    /// <summary>
    /// Returns the <see cref="ToolProperties"/> of the currently held item,
    /// or null if the held item is not a tool or the slot is empty.
    /// </summary>
    /// <returns>The tool properties, or null.</returns>
    public ToolProperties? GetSelectedToolProperties()
    {
        ItemInstance? item = GetSelectedItem();

        if (item is null)
        {
            return null;
        }

        if (!_itemRegistry.TryGet(item.DefinitionId, out ItemDefinition definition))
        {
            return null;
        }

        return definition.Tool;
    }
}
