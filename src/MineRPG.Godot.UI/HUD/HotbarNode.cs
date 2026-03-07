using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces.Gameplay;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.RPG.Drops;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

using MineRPG.Game.Bootstrap.Input;
using MineRPG.Godot.UI.Inventory;
using MineRPG.Godot.UI.Items;

namespace MineRPG.Godot.UI.HUD;

/// <summary>
/// 9-slot hotbar displayed at the bottom center of the screen.
/// Creates <see cref="InventorySlotNode"/> instances programmatically and manages
/// scroll wheel selection, number key selection (1-9), and item dropping (Q / Ctrl+Q).
/// </summary>
public sealed partial class HotbarNode : Control
{
    private const int SlotCount = 9;
    private const string SlotScenePath = "res://Scenes/UI/Widgets/InventorySlot.tscn";

    [Export] private HBoxContainer _slotContainer = null!;

    private readonly InventorySlotNode[] _slotNodes = new InventorySlotNode[SlotCount];

    private int _selectedIndex;
    private IHotbarController _hotbar = null!;
    private ILogger _logger = null!;
    private IEventBus _eventBus = null!;
    private PlayerInventory _playerInventory = null!;
    private PlayerData _playerData = null!;
    private bool _inventoryOpen;

    /// <inheritdoc />
    public override void _Ready()
    {
        _hotbar = ServiceLocator.Instance.Get<IHotbarController>();
        _logger = ServiceLocator.Instance.Get<ILogger>();
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _playerInventory = ServiceLocator.Instance.Get<PlayerInventory>();
        _playerData = ServiceLocator.Instance.Get<PlayerData>();
        ItemRegistry itemRegistry = ServiceLocator.Instance.Get<ItemRegistry>();

        ItemIconAtlas? iconAtlas = null;

        if (ServiceLocator.Instance.TryGet<ItemIconAtlas>(out ItemIconAtlas? atlas))
        {
            iconAtlas = atlas;
        }

        _eventBus.Subscribe<InventoryToggledEvent>(OnInventoryToggled);

        // [Export] node references may not auto-resolve from NodePath in .tscn;
        // fallback to GetNode for reliable resolution.
        _slotContainer ??= GetNode<HBoxContainer>("SlotContainer");

        PackedScene slotScene = GD.Load<PackedScene>(SlotScenePath);

        for (int i = 0; i < SlotCount; i++)
        {
            InventorySlotNode slotNode = slotScene.Instantiate<InventorySlotNode>();
            slotNode.Name = $"Slot{i}";
            _slotContainer.AddChild(slotNode);
            slotNode.Initialize(_playerInventory.Hotbar, i, itemRegistry, iconAtlas);
            _slotNodes[i] = slotNode;
        }

        _slotNodes[_selectedIndex].SetSelected();

        _logger.Info("HotbarNode ready -- {0} slots.", SlotCount);
    }

    /// <inheritdoc />
    public override void _ExitTree() => _eventBus.Unsubscribe<InventoryToggledEvent>(OnInventoryToggled);

    /// <inheritdoc />
    public override void _Input(InputEvent @event)
    {
        if (_inventoryOpen)
        {
            return;
        }

        HandleScrollWheel(@event);
        HandleNumberKeys(@event);
        HandleDropInput(@event);
    }

    private void HandleScrollWheel(InputEvent @event)
    {
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

        SelectSlot((_selectedIndex + direction + SlotCount) % SlotCount);
        GetViewport().SetInputAsHandled();
    }

    private void HandleNumberKeys(InputEvent @event)
    {
        if (@event is not InputEventKey keyEvent || !keyEvent.Pressed || keyEvent.Echo)
        {
            return;
        }

        int slotIndex = keyEvent.PhysicalKeycode switch
        {
            Key.Key1 => 0,
            Key.Key2 => 1,
            Key.Key3 => 2,
            Key.Key4 => 3,
            Key.Key5 => 4,
            Key.Key6 => 5,
            Key.Key7 => 6,
            Key.Key8 => 7,
            Key.Key9 => 8,
            _ => -1,
        };

        if (slotIndex < 0)
        {
            return;
        }

        SelectSlot(slotIndex);
        GetViewport().SetInputAsHandled();
    }

    private void HandleDropInput(InputEvent @event)
    {
        // Check Ctrl+Q (drop stack) first — if matched, do not also trigger single drop
        bool dropAll = @event.IsActionPressed(InputActions.DropStack);
        bool dropOne = !dropAll && @event.IsActionPressed(InputActions.DropItem);

        if (!dropOne && !dropAll)
        {
            return;
        }

        ItemInstance? held = _playerInventory.Hotbar.GetSlot(_selectedIndex);

        if (held is null)
        {
            return;
        }

        int countToDrop = dropAll ? held.Count : 1;

        ItemInstance? removed = _playerInventory.Hotbar.RemoveAt(_selectedIndex, countToDrop);

        if (removed is null)
        {
            return;
        }

        float spawnX = _playerData.PositionX;
        float spawnY = _playerData.PositionY + 1.0f;
        float spawnZ = _playerData.PositionZ;

        _eventBus.Publish(new ItemDropSpawnedEvent
        {
            X = spawnX,
            Y = spawnY,
            Z = spawnZ,
            ItemDefinitionId = removed.DefinitionId,
            Count = removed.Count,
            VelocityX = DropVelocity.PlayerThrow.X,
            VelocityY = DropVelocity.PlayerThrow.Y,
            VelocityZ = DropVelocity.PlayerThrow.Z,
            PlayerYaw = _playerData.CameraYaw,
        });

        GetViewport().SetInputAsHandled();
    }

    private void SelectSlot(int newIndex)
    {
        int oldIndex = _selectedIndex;
        _selectedIndex = newIndex;
        _hotbar.SelectSlot(_selectedIndex);

        _slotNodes[oldIndex].SetNormal();
        _slotNodes[newIndex].SetSelected();
    }

    private void OnInventoryToggled(InventoryToggledEvent e) => _inventoryOpen = e.IsOpen;
}
