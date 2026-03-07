using System;

using Godot;

using MineRPG.Godot.UI.Items;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

using InventoryContainer = MineRPG.RPG.Inventory.Inventory;

namespace MineRPG.Godot.UI.Inventory;

/// <summary>
/// A single inventory slot UI widget (50x50 PanelContainer).
/// Displays the item icon (atlas texture or placeholder color) and stack count.
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
    private ItemIconAtlas? _iconAtlas;
    private TextureRect _iconTexture = null!;
    private ColorRect _iconColorFallback = null!;
    private Label _countLabel = null!;
    private StyleBoxFlat _normalStyle = null!;
    private StyleBoxFlat _hoverStyle = null!;
    private StyleBoxFlat _selectedStyle = null!;
    private bool _isSelected;

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
    /// <param name="iconAtlas">Optional item icon atlas for textured icons.</param>
    public void Initialize(
        InventoryContainer inventory,
        int slotIndex,
        ItemRegistry itemRegistry,
        ItemIconAtlas? iconAtlas = null)
    {
        _inventory = inventory;
        _slotIndex = slotIndex;
        _itemRegistry = itemRegistry;
        _iconAtlas = iconAtlas;

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

        _selectedStyle = new StyleBoxFlat();
        _selectedStyle.BgColor = GameTheme.SlotBackground;
        _selectedStyle.SetBorderWidthAll(GameTheme.BorderWidth);
        _selectedStyle.BorderColor = GameTheme.SlotSelectedBorder;
        _selectedStyle.SetContentMarginAll(0);

        AddThemeStyleboxOverride("panel", _normalStyle);

        _iconTexture = new TextureRect();
        _iconTexture.CustomMinimumSize = new Vector2(IconSize, IconSize);
        _iconTexture.Position = new Vector2(IconMargin, IconMargin);
        _iconTexture.Size = new Vector2(IconSize, IconSize);
        _iconTexture.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        _iconTexture.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _iconTexture.MouseFilter = MouseFilterEnum.Ignore;
        _iconTexture.Visible = false;
        AddChild(_iconTexture);

        _iconColorFallback = new ColorRect();
        _iconColorFallback.CustomMinimumSize = new Vector2(IconSize, IconSize);
        _iconColorFallback.Position = new Vector2(IconMargin, IconMargin);
        _iconColorFallback.Size = new Vector2(IconSize, IconSize);
        _iconColorFallback.MouseFilter = MouseFilterEnum.Ignore;
        _iconColorFallback.Color = Colors.Transparent;
        AddChild(_iconColorFallback);

        _countLabel = new Label();
        _countLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _countLabel.VerticalAlignment = VerticalAlignment.Bottom;
        _countLabel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _countLabel.OffsetRight = -2;
        _countLabel.OffsetBottom = -1;
        _countLabel.ThemeTypeVariation = ThemeTypeVariations.SlotCountLabel;
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
            _iconTexture.Texture = null;
            _iconTexture.Visible = false;
            _iconColorFallback.Color = Colors.Transparent;
            _countLabel.Text = "";
            return;
        }

        if (_itemRegistry.TryGet(item.DefinitionId, out ItemDefinition definition))
        {
            AtlasTexture? atlas = _iconAtlas?.GetIconTexture(definition.IconAtlasId);

            if (atlas is not null)
            {
                _iconTexture.Texture = atlas;
                _iconTexture.Visible = true;
                _iconColorFallback.Color = Colors.Transparent;
            }
            else
            {
                _iconTexture.Texture = null;
                _iconTexture.Visible = false;
                _iconColorFallback.Color = GameTheme.GetCategoryPlaceholderColor(definition.Category);
            }

            _countLabel.Text = item.Count > 1 ? item.Count.ToString() : "";
        }
        else
        {
            _iconTexture.Texture = null;
            _iconTexture.Visible = false;
            _iconColorFallback.Color = new Color(1.0f, 0.0f, 1.0f, 0.8f);
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
    /// Skipped when the slot is in the selected state (selected takes priority).
    /// </summary>
    public void SetHovered()
    {
        if (!_isSelected)
        {
            AddThemeStyleboxOverride("panel", _hoverStyle);
        }
    }

    /// <summary>
    /// Sets the slot border style to the normal appearance and clears the selected state.
    /// </summary>
    public void SetNormal()
    {
        _isSelected = false;
        AddThemeStyleboxOverride("panel", _normalStyle);
    }

    /// <summary>
    /// Sets the slot border style to the selected appearance (thicker border).
    /// </summary>
    public void SetSelected()
    {
        _isSelected = true;
        AddThemeStyleboxOverride("panel", _selectedStyle);
    }
}
