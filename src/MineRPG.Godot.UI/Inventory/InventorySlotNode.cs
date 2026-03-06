using System;

using Godot;

using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

using InventoryContainer = MineRPG.RPG.Inventory.Inventory;

namespace MineRPG.Godot.UI.Inventory;

/// <summary>
/// A single inventory slot UI widget (50x50 PanelContainer).
/// Displays the item icon (placeholder colored rect) and stack count.
/// Fires events for mouse interaction (click, hover).
/// Subscribes to <see cref="RPG.Inventory.Inventory.SlotChanged"/> to auto-refresh.
/// </summary>
public sealed partial class InventorySlotNode : PanelContainer
{
    private const int SlotSize = 50;
    private const int IconMargin = 4;
    private const int IconSize = SlotSize - IconMargin * 2;

    private InventoryContainer? _inventory;
    private int _slotIndex;
    private ItemRegistry _itemRegistry = null!;
    private ColorRect _iconRect = null!;
    private Label _countLabel = null!;
    private StyleBoxFlat _normalStyle = null!;
    private StyleBoxFlat _hoverStyle = null!;

    /// <summary>Raised when this slot is clicked.</summary>
    public event EventHandler<SlotClickedEventArgs>? SlotClicked;

    /// <summary>Raised when the mouse enters this slot.</summary>
    public event EventHandler<SlotHoverEventArgs>? SlotHovered;

    /// <summary>Raised when the mouse exits this slot.</summary>
    public event EventHandler<SlotHoverEventArgs>? SlotUnhovered;

    /// <summary>The inventory this slot belongs to.</summary>
    public InventoryContainer? BoundInventory => _inventory;

    /// <summary>The slot index within the bound inventory.</summary>
    public int SlotIndex => _slotIndex;

    /// <summary>
    /// Initializes the slot node, binding it to a specific inventory and slot index.
    /// Must be called after the node is added to the tree.
    /// </summary>
    /// <param name="inventory">The inventory containing this slot.</param>
    /// <param name="slotIndex">The index within the inventory.</param>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    public void Initialize(InventoryContainer inventory, int slotIndex, ItemRegistry itemRegistry)
    {
        _inventory = inventory;
        _slotIndex = slotIndex;
        _itemRegistry = itemRegistry;

        _inventory.SlotChanged += OnSlotChanged;

        BuildUI();
        Refresh();
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        if (_inventory != null)
        {
            _inventory.SlotChanged -= OnSlotChanged;
        }
    }

    /// <inheritdoc />
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            SlotClicked?.Invoke(this, new SlotClickedEventArgs(this, mouseButton.ButtonIndex));
            AcceptEvent();
        }
    }

    private void BuildUI()
    {
        CustomMinimumSize = new Vector2(SlotSize, SlotSize);
        MouseFilter = MouseFilterEnum.Stop;

        _normalStyle = new StyleBoxFlat();
        _normalStyle.BgColor = GameTheme.SlotBackground;
        _normalStyle.SetBorderWidthAll(GameTheme.BorderWidthThin);
        _normalStyle.BorderColor = GameTheme.SlotBorder;
        _normalStyle.SetContentMarginAll(0);

        _hoverStyle = new StyleBoxFlat();
        _hoverStyle.BgColor = GameTheme.SlotBackground;
        _hoverStyle.SetBorderWidthAll(GameTheme.BorderWidthThin);
        _hoverStyle.BorderColor = GameTheme.SlotHoverBorder;
        _hoverStyle.SetContentMarginAll(0);

        AddThemeStyleboxOverride("panel", _normalStyle);

        _iconRect = new ColorRect();
        _iconRect.CustomMinimumSize = new Vector2(IconSize, IconSize);
        _iconRect.Position = new Vector2(IconMargin, IconMargin);
        _iconRect.Size = new Vector2(IconSize, IconSize);
        _iconRect.MouseFilter = MouseFilterEnum.Ignore;
        _iconRect.Color = Colors.Transparent;
        AddChild(_iconRect);

        _countLabel = new Label();
        _countLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _countLabel.VerticalAlignment = VerticalAlignment.Bottom;
        _countLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _countLabel.OffsetRight = -2;
        _countLabel.OffsetBottom = -1;
        _countLabel.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeSmall);
        _countLabel.AddThemeColorOverride("font_color", GameTheme.TextTitle);
        _countLabel.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_countLabel);

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private void Refresh()
    {
        if (_inventory == null)
        {
            return;
        }

        ItemInstance? item = _inventory.GetSlot(_slotIndex);

        if (item == null)
        {
            _iconRect.Color = Colors.Transparent;
            _countLabel.Text = "";
            return;
        }

        if (_itemRegistry.TryGet(item.DefinitionId, out ItemDefinition definition))
        {
            _iconRect.Color = GameTheme.GetCategoryPlaceholderColor(definition.Category);
            _countLabel.Text = item.Count > 1 ? item.Count.ToString() : "";
        }
        else
        {
            _iconRect.Color = new Color(1.0f, 0.0f, 1.0f, 0.8f);
            _countLabel.Text = item.Count > 1 ? item.Count.ToString() : "";
        }
    }

    private void OnSlotChanged(object? sender, SlotChangedEventArgs args)
    {
        if (args.SlotIndex == _slotIndex)
        {
            Refresh();
        }
    }

    private void OnMouseEntered() => SlotHovered?.Invoke(this, new SlotHoverEventArgs(this));

    private void OnMouseExited() => SlotUnhovered?.Invoke(this, new SlotHoverEventArgs(this));

    /// <summary>
    /// Sets the slot border style to the hovered appearance.
    /// </summary>
    public void SetHovered() => AddThemeStyleboxOverride("panel", _hoverStyle);

    /// <summary>
    /// Sets the slot border style to the normal appearance.
    /// </summary>
    public void SetNormal() => AddThemeStyleboxOverride("panel", _normalStyle);
}
