using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Game.Bootstrap.Input;
using MineRPG.Godot.UI.Items;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

namespace MineRPG.Godot.UI.Inventory;

/// <summary>
/// Root Control for the full-screen inventory overlay.
/// Layout is defined in Scenes/UI/Inventory.tscn.
/// Handles E / Escape input for toggling, initializes slot grids,
/// manages cursor item, and publishes <see cref="InventoryToggledEvent"/>.
/// </summary>
public sealed partial class InventoryScreenNode : Control
{
    [Export] private ColorRect _overlay = null!;
    [Export] private ArmorPanelNode _armorPanel = null!;
    [Export] private InventoryGridNode _mainGrid = null!;
    [Export] private InventoryGridNode _hotbarGrid = null!;
    [Export] private CursorItemNode _cursorItemNode = null!;
    [Export] private ItemTooltipNode _tooltipNode = null!;

    private ILogger _logger = null!;
    private IEventBus _eventBus = null!;
    private PlayerInventory _playerInventory = null!;
    private CursorItemHolder _cursor = null!;
    private ItemRegistry _itemRegistry = null!;
    private ItemIconAtlas? _iconAtlas;

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

        if (ServiceLocator.Instance.TryGet<ItemIconAtlas>(out ItemIconAtlas? atlas))
        {
            _iconAtlas = atlas;
        }

        InitializeChildren();

        Visible = false;
        _isOpen = false;

        _logger.Info("InventoryScreenNode ready.");
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed(InputActions.InventoryToggle))
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

        if (_isOpen && @event.IsActionPressed(InputActions.Pause))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        _armorPanel.SlotClicked -= OnSlotClicked;
        _armorPanel.SlotHovered -= OnSlotHovered;
        _armorPanel.SlotUnhovered -= OnSlotUnhovered;

        _mainGrid.SlotClicked -= OnSlotClicked;
        _mainGrid.SlotHovered -= OnSlotHovered;
        _mainGrid.SlotUnhovered -= OnSlotUnhovered;

        _hotbarGrid.SlotClicked -= OnSlotClicked;
        _hotbarGrid.SlotHovered -= OnSlotHovered;
        _hotbarGrid.SlotUnhovered -= OnSlotUnhovered;
    }

    private void InitializeChildren()
    {
        _armorPanel.Initialize(
            _playerInventory.Armor, _playerInventory.Offhand, _itemRegistry, _iconAtlas);
        _armorPanel.SlotClicked += OnSlotClicked;
        _armorPanel.SlotHovered += OnSlotHovered;
        _armorPanel.SlotUnhovered += OnSlotUnhovered;

        _mainGrid.Initialize(_playerInventory.Main, 9, _itemRegistry, _iconAtlas);
        _mainGrid.SlotClicked += OnSlotClicked;
        _mainGrid.SlotHovered += OnSlotHovered;
        _mainGrid.SlotUnhovered += OnSlotUnhovered;

        _hotbarGrid.Initialize(_playerInventory.Hotbar, 9, _itemRegistry, _iconAtlas);
        _hotbarGrid.SlotClicked += OnSlotClicked;
        _hotbarGrid.SlotHovered += OnSlotHovered;
        _hotbarGrid.SlotUnhovered += OnSlotUnhovered;

        _cursorItemNode.Initialize(_cursor, _itemRegistry, _iconAtlas);
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
        else
        {
            _tooltipNode.HideTooltip();
        }
    }

    private void OnSlotUnhovered(object? sender, SlotHoverEventArgs e)
    {
        e.Slot.SetNormal();
        _tooltipNode.HideTooltip();
    }
}
