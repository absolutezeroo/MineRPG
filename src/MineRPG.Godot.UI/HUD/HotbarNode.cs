using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.HUD;

/// <summary>
/// 9-slot hotbar displayed at the bottom center of the screen.
/// Slots are empty panels for now (no item system wired yet).
/// Scroll wheel changes the selected slot via <see cref="IHotbarController"/>.
/// </summary>
public sealed partial class HotbarNode : Control
{
    private const int SlotCount = 9;
    private const float SlotSize = 50f;
    private const float SlotSpacing = 4f;
    private const float BottomMargin = 12f;
    private const float BorderWidth = 2f;
    private const float CenterMultiplier = 0.5f;

    private static readonly Color SlotBackgroundColor = new(0.15f, 0.15f, 0.15f, 0.75f);
    private static readonly Color SlotBorderColor = new(0.5f, 0.5f, 0.5f, 0.85f);
    private static readonly Color SlotSelectedBorderColor = new(1f, 1f, 1f, 0.95f);

    private readonly Rect2[] _slotRects = new Rect2[SlotCount];

    private int _selectedIndex;
    private IHotbarController _hotbar = null!;
    private ILogger _logger = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _hotbar = ServiceLocator.Instance.Get<IHotbarController>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore;

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
            _selectedIndex = (_selectedIndex - 1 + SlotCount) % SlotCount;
            _hotbar.SelectSlot(_selectedIndex);
            QueueRedraw();
            GetViewport().SetInputAsHandled();
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
        {
            _selectedIndex = (_selectedIndex + 1) % SlotCount;
            _hotbar.SelectSlot(_selectedIndex);
            QueueRedraw();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <inheritdoc />
    public override void _Notification(int what)
    {
        if (what == NotificationResized)
        {
            QueueRedraw();
        }
    }

    /// <inheritdoc />
    public override void _Draw()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        float totalWidth = SlotCount * SlotSize + (SlotCount - 1) * SlotSpacing;
        float startX = (viewportSize.X - totalWidth) * CenterMultiplier;
        float startY = viewportSize.Y - SlotSize - BottomMargin;

        for (int i = 0; i < SlotCount; i++)
        {
            Rect2 rect = new(
                startX + i * (SlotSize + SlotSpacing),
                startY,
                SlotSize,
                SlotSize);

            _slotRects[i] = rect;

            DrawRect(rect, SlotBackgroundColor);

            Color borderColor = i == _selectedIndex ? SlotSelectedBorderColor : SlotBorderColor;
            DrawRect(rect, borderColor, false, BorderWidth);
        }
    }
}
