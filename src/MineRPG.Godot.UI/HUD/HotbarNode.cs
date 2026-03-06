using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.HUD;

/// <summary>
/// 9-slot hotbar displayed at the bottom center of the screen.
/// Layout is defined in Scenes/UI/HUD/Hotbar.tscn; this script handles
/// scroll wheel selection and updates slot border styles via cached style overrides.
/// </summary>
public sealed partial class HotbarNode : Control
{
    private const int SlotCount = 9;

    private readonly PanelContainer[] _slots = new PanelContainer[SlotCount];

    private StyleBoxFlat _slotStyleNormal = null!;
    private StyleBoxFlat _slotStyleSelected = null!;
    private int _selectedIndex;
    private IHotbarController _hotbar = null!;
    private ILogger _logger = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _hotbar = ServiceLocator.Instance.Get<IHotbarController>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        _slotStyleNormal = CreateSlotStyle(isSelected: false);
        _slotStyleSelected = CreateSlotStyle(isSelected: true);

        for (int i = 0; i < SlotCount; i++)
        {
            _slots[i] = GetNode<PanelContainer>($"SlotContainer/Slot{i}");
            _slots[i].AddThemeStyleboxOverride("panel", _slotStyleNormal);
        }

        _slots[_selectedIndex].AddThemeStyleboxOverride("panel", _slotStyleSelected);

        _logger.Info("HotbarNode ready -- {0} slots.", SlotCount);
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.WheelUp)
        {
            int oldIndex = _selectedIndex;
            _selectedIndex = (_selectedIndex - 1 + SlotCount) % SlotCount;
            _hotbar.SelectSlot(_selectedIndex);
            SwapSlotStyles(oldIndex, _selectedIndex);
            GetViewport().SetInputAsHandled();
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
        {
            int oldIndex = _selectedIndex;
            _selectedIndex = (_selectedIndex + 1) % SlotCount;
            _hotbar.SelectSlot(_selectedIndex);
            SwapSlotStyles(oldIndex, _selectedIndex);
            GetViewport().SetInputAsHandled();
        }
    }

    private void SwapSlotStyles(int oldIndex, int newIndex)
    {
        _slots[oldIndex].AddThemeStyleboxOverride("panel", _slotStyleNormal);
        _slots[newIndex].AddThemeStyleboxOverride("panel", _slotStyleSelected);
    }

    private static StyleBoxFlat CreateSlotStyle(bool isSelected)
    {
        StyleBoxFlat style = new();
        style.BgColor = GameTheme.SlotBackground;
        style.SetBorderWidthAll(GameTheme.BorderWidth);
        style.BorderColor = isSelected ? GameTheme.SlotSelectedBorder : GameTheme.SlotBorder;
        return style;
    }
}
