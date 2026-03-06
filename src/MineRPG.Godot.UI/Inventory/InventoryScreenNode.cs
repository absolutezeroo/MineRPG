using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Godot.UI.Inventory;

/// <summary>
/// Root Control for the full-screen inventory overlay.
/// Handles E / Escape input for toggling, creates slot grids programmatically,
/// manages cursor item, and publishes <see cref="InventoryToggledEvent"/>.
/// </summary>
public sealed partial class InventoryScreenNode : Control
{
    private ILogger _logger = null!;
    private IEventBus _eventBus = null!;
    private PlayerInventory _playerInventory = null!;
    private CursorItemHolder _cursor = null!;
    private ItemRegistry _itemRegistry = null!;

    private InventoryGridNode _mainGrid = null!;
    private InventoryGridNode _hotbarGrid = null!;
    private ArmorPanelNode _armorPanel = null!;
    private CursorItemNode _cursorItemNode = null!;
    private ItemTooltipNode _tooltipNode = null!;

    private bool _isOpen;

    /// <summary>Whether the inventory screen is currently visible.</summary>
    public bool IsOpen => _isOpen;

    /// <inheritdoc />
    public override void _Ready()
    {
        _logger = ServiceLocator.Instance.Get<ILogger>();
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _playerInventory = ServiceLocator.Instance.Get<PlayerInventory>();
        _cursor = ServiceLocator.Instance.Get<CursorItemHolder>();
        _itemRegistry = ServiceLocator.Instance.Get<ItemRegistry>();

        BuildContent();

        Visible = false;
        _isOpen = false;

        _logger.Info("InventoryScreenNode ready.");
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed(InputActionNames.InventoryToggle))
        {
            if (_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }

            GetViewport().SetInputAsHandled();
            return;
        }

        if (_isOpen && @event.IsActionPressed(InputActionNames.Pause))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Open()
    {
        _isOpen = true;
        Visible = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _eventBus.Publish(new InventoryToggledEvent { IsOpen = true });
    }

    private void Close()
    {
        ReturnCursorItemToInventory();
        _tooltipNode.HideTooltip();

        _isOpen = false;
        Visible = false;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _eventBus.Publish(new InventoryToggledEvent { IsOpen = false });
    }

    private void ReturnCursorItemToInventory()
    {
        if (_cursor.IsEmpty)
        {
            return;
        }

        ItemInstance? heldItem = _cursor.TakeItem();

        if (heldItem == null)
        {
            return;
        }

        int remaining = _playerInventory.AddItem(heldItem);

        if (remaining > 0)
        {
            _logger.Warning(
                "InventoryScreenNode: Could not return {0}x {1} to inventory (full).",
                remaining, heldItem.DefinitionId);
        }
    }

    private void BuildContent()
    {
        // Overlay background
        ColorRect overlay = new();
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        overlay.Color = GameTheme.Overlay;
        overlay.MouseFilter = MouseFilterEnum.Stop;
        AddChild(overlay);

        // Center container
        CenterContainer centerContainer = new();
        centerContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        centerContainer.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(centerContainer);

        // Main panel
        PanelContainer mainPanel = new();
        mainPanel.MouseFilter = MouseFilterEnum.Stop;
        centerContainer.AddChild(mainPanel);

        // Horizontal layout: armor | center column
        HBoxContainer hbox = new();
        hbox.AddThemeConstantOverride("separation", 16);
        hbox.MouseFilter = MouseFilterEnum.Ignore;
        mainPanel.AddChild(hbox);

        // Armor column
        _armorPanel = new ArmorPanelNode();
        _armorPanel.Name = "ArmorPanel";
        _armorPanel.MouseFilter = MouseFilterEnum.Ignore;
        hbox.AddChild(_armorPanel);
        _armorPanel.Initialize(_playerInventory.Armor, _playerInventory.Offhand, _itemRegistry);
        _armorPanel.SlotClicked += OnSlotClicked;
        _armorPanel.SlotHovered += OnSlotHovered;
        _armorPanel.SlotUnhovered += OnSlotUnhovered;

        // Center column
        VBoxContainer centerColumn = new();
        centerColumn.AddThemeConstantOverride("separation", 8);
        centerColumn.MouseFilter = MouseFilterEnum.Ignore;
        hbox.AddChild(centerColumn);

        // Title
        Label titleLabel = new();
        titleLabel.Text = "Inventory";
        titleLabel.HorizontalAlignment = HorizontalAlignment.Center;
        titleLabel.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeSubTitle);
        titleLabel.AddThemeColorOverride("font_color", GameTheme.TextTitle);
        centerColumn.AddChild(titleLabel);

        HSeparator sep1 = new();
        centerColumn.AddChild(sep1);

        // Main grid (3 rows x 9 cols = 27 slots)
        _mainGrid = new InventoryGridNode();
        _mainGrid.Name = "MainGrid";
        centerColumn.AddChild(_mainGrid);
        _mainGrid.Initialize(_playerInventory.Main, 9, _itemRegistry);
        _mainGrid.SlotClicked += OnSlotClicked;
        _mainGrid.SlotHovered += OnSlotHovered;
        _mainGrid.SlotUnhovered += OnSlotUnhovered;

        HSeparator sep2 = new();
        centerColumn.AddChild(sep2);

        // Hotbar label
        Label hotbarLabel = new();
        hotbarLabel.Text = "Hotbar";
        hotbarLabel.HorizontalAlignment = HorizontalAlignment.Center;
        hotbarLabel.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeBody);
        hotbarLabel.AddThemeColorOverride("font_color", GameTheme.TextSub);
        centerColumn.AddChild(hotbarLabel);

        // Hotbar grid (1 row x 9 cols)
        _hotbarGrid = new InventoryGridNode();
        _hotbarGrid.Name = "HotbarGrid";
        centerColumn.AddChild(_hotbarGrid);
        _hotbarGrid.Initialize(_playerInventory.Hotbar, 9, _itemRegistry);
        _hotbarGrid.SlotClicked += OnSlotClicked;
        _hotbarGrid.SlotHovered += OnSlotHovered;
        _hotbarGrid.SlotUnhovered += OnSlotUnhovered;

        // Cursor follower
        _cursorItemNode = new CursorItemNode();
        _cursorItemNode.Name = "CursorItem";
        AddChild(_cursorItemNode);
        _cursorItemNode.Initialize(_cursor, _itemRegistry);

        // Tooltip
        _tooltipNode = new ItemTooltipNode();
        _tooltipNode.Name = "ItemTooltip";
        AddChild(_tooltipNode);
    }

    private void OnSlotClicked(object? sender, SlotClickedEventArgs e)
    {
        InventorySlotNode slot = e.Slot;
        MouseButton button = e.Button;

        if (slot.BoundInventory == null)
        {
            return;
        }

        bool isShift = Input.IsKeyPressed(Key.Shift);

        if (isShift && button == MouseButton.Left)
        {
            InventorySlotInteraction.HandleShiftClick(
                _playerInventory,
                slot.BoundInventory,
                slot.SlotIndex,
                _itemRegistry);
            return;
        }

        if (button == MouseButton.Left)
        {
            InventorySlotInteraction.HandleLeftClick(
                slot.BoundInventory,
                slot.SlotIndex,
                _cursor,
                _itemRegistry);
        }
        else if (button == MouseButton.Right)
        {
            InventorySlotInteraction.HandleRightClick(
                slot.BoundInventory,
                slot.SlotIndex,
                _cursor,
                _itemRegistry);
        }
    }

    private void OnSlotHovered(object? sender, SlotHoverEventArgs e)
    {
        InventorySlotNode slot = e.Slot;
        slot.SetHovered();

        if (slot.BoundInventory == null)
        {
            return;
        }

        ItemInstance? item = slot.BoundInventory.GetSlot(slot.SlotIndex);

        if (item == null)
        {
            _tooltipNode.HideTooltip();
            return;
        }

        if (_itemRegistry.TryGet(item.DefinitionId, out ItemDefinition definition))
        {
            _tooltipNode.ShowForItem(definition, item);
        }
    }

    private void OnSlotUnhovered(object? sender, SlotHoverEventArgs e)
    {
        e.Slot.SetNormal();
        _tooltipNode.HideTooltip();
    }
}
