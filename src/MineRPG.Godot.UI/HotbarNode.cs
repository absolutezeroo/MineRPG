using Godot;
using MineRPG.Core.DI;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;

namespace MineRPG.Godot.UI;

/// <summary>
/// 9-slot hotbar displayed at the bottom center of the screen.
/// Slots are empty panels for now (no item system wired yet).
/// Scroll wheel changes the selected slot and updates PlayerData.SelectedBlockId.
/// </summary>
public sealed partial class HotbarNode : Control
{
    private const int SlotCount = 9;
    private const float SlotSize = 50f;
    private const float SlotSpacing = 4f;
    private const float BottomMargin = 12f;
    private const float BorderWidth = 2f;

    private static readonly Color SlotBackgroundColor = new(0.15f, 0.15f, 0.15f, 0.75f);
    private static readonly Color SlotBorderColor = new(0.5f, 0.5f, 0.5f, 0.85f);
    private static readonly Color SlotSelectedBorderColor = new(1f, 1f, 1f, 0.95f);

    private readonly Rect2[] _slotRects = new Rect2[SlotCount];
    private int _selectedIndex;

    private PlayerData _playerData = null!;
    private ILogger _logger = null!;

    public override void _Ready()
    {
        _playerData = ServiceLocator.Instance.Get<PlayerData>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

        _logger.Info("HotbarNode ready — {0} slots.", SlotCount);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseBtn || !mouseBtn.Pressed)
            return;

        if (mouseBtn.ButtonIndex == MouseButton.WheelUp)
        {
            _selectedIndex = (_selectedIndex - 1 + SlotCount) % SlotCount;
            _playerData.SelectedBlockId = (ushort)(_selectedIndex + 1);
            QueueRedraw();
            GetViewport().SetInputAsHandled();
        }
        else if (mouseBtn.ButtonIndex == MouseButton.WheelDown)
        {
            _selectedIndex = (_selectedIndex + 1) % SlotCount;
            _playerData.SelectedBlockId = (ushort)(_selectedIndex + 1);
            QueueRedraw();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Draw()
    {
        var viewportSize = GetViewportRect().Size;
        var totalWidth = SlotCount * SlotSize + (SlotCount - 1) * SlotSpacing;
        var startX = (viewportSize.X - totalWidth) * 0.5f;
        var startY = viewportSize.Y - SlotSize - BottomMargin;

        for (var i = 0; i < SlotCount; i++)
        {
            var rect = new Rect2(
                startX + i * (SlotSize + SlotSpacing),
                startY,
                SlotSize,
                SlotSize);

            _slotRects[i] = rect;

            DrawRect(rect, SlotBackgroundColor);

            var borderColor = i == _selectedIndex ? SlotSelectedBorderColor : SlotBorderColor;
            DrawRect(rect, borderColor, false, BorderWidth);
        }
    }
}
