using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

using MineRPG.Godot.UI.Inventory;

namespace MineRPG.Godot.UI.HUD;

/// <summary>
/// 9-slot hotbar displayed at the bottom center of the screen.
/// Creates <see cref="InventorySlotNode"/> instances programmatically and manages
/// scroll wheel selection.
/// </summary>
public sealed partial class HotbarNode : Control
{
    private const int SlotCount = 9;

    private readonly InventorySlotNode[] _slotNodes = new InventorySlotNode[SlotCount];

    private int _selectedIndex;
    private IHotbarController _hotbar = null!;
    private ILogger _logger = null!;
    private IEventBus _eventBus = null!;
    private bool _inventoryOpen;

    /// <inheritdoc />
    public override void _Ready()
    {
        _hotbar = ServiceLocator.Instance.Get<IHotbarController>();
        _logger = ServiceLocator.Instance.Get<ILogger>();
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        PlayerInventory playerInventory = ServiceLocator.Instance.Get<PlayerInventory>();
        ItemRegistry itemRegistry = ServiceLocator.Instance.Get<ItemRegistry>();

        _eventBus.Subscribe<InventoryToggledEvent>(OnInventoryToggled);

        GameTheme.Apply(this);

        HBoxContainer slotContainer = GetNode<HBoxContainer>("SlotContainer");

        for (int i = 0; i < SlotCount; i++)
        {
            InventorySlotNode slotNode = new();
            slotNode.Name = $"Slot{i}";
            slotContainer.AddChild(slotNode);
            slotNode.Initialize(playerInventory.Hotbar, i, itemRegistry);
            _slotNodes[i] = slotNode;
        }

        _slotNodes[_selectedIndex].SetSelected();

        _logger.Info("HotbarNode ready -- {0} slots.", SlotCount);
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        _eventBus.Unsubscribe<InventoryToggledEvent>(OnInventoryToggled);
    }

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (_inventoryOpen)
        {
            return;
        }

        if (@event is not InputEventMouseButton mouseButton || !mouseButton.Pressed)
        {
            return;
        }

        int direction = mouseButton.ButtonIndex switch
        {
            MouseButton.WheelUp => -1,
            MouseButton.WheelDown => 1,
            _ => 0,
        };

        if (direction == 0)
        {
            return;
        }

        int oldIndex = _selectedIndex;
        _selectedIndex = (_selectedIndex + direction + SlotCount) % SlotCount;
        _hotbar.SelectSlot(_selectedIndex);
        SwapSlotSelection(oldIndex, _selectedIndex);
        GetViewport().SetInputAsHandled();
    }

    private void SwapSlotSelection(int oldIndex, int newIndex)
    {
        _slotNodes[oldIndex].SetNormal();
        _slotNodes[newIndex].SetSelected();
    }

    private void OnInventoryToggled(InventoryToggledEvent e) => _inventoryOpen = e.IsOpen;
}
