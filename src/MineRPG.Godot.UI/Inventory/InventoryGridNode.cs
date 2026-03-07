using System;

using Godot;

using MineRPG.Godot.UI.Items;
using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

using InventoryContainer = MineRPG.RPG.Inventory.Inventory;

namespace MineRPG.Godot.UI.Inventory;

/// <summary>
/// A grid of <see cref="InventorySlotNode"/> children, arranged in a
/// <see cref="GridContainer"/> with the specified column count.
/// Relays slot click and hover events upward.
/// </summary>
public sealed partial class InventoryGridNode : GridContainer
{
    private const int SlotSeparation = 2;
    private const string SlotScenePath = "res://Scenes/UI/Widgets/InventorySlot.tscn";

    private static PackedScene? _slotSceneCache;
    private InventorySlotNode[] _slotNodes = [];

    /// <summary>Raised when any child slot is clicked.</summary>
    public event EventHandler<SlotClickedEventArgs>? SlotClicked;

    /// <summary>Raised when the mouse enters any child slot.</summary>
    public event EventHandler<SlotHoverEventArgs>? SlotHovered;

    /// <summary>Raised when the mouse exits any child slot.</summary>
    public event EventHandler<SlotHoverEventArgs>? SlotUnhovered;

    /// <summary>
    /// Builds the grid with one <see cref="InventorySlotNode"/> per inventory slot.
    /// </summary>
    /// <param name="inventory">The inventory to bind to.</param>
    /// <param name="columns">Number of columns in the grid.</param>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    /// <param name="iconAtlas">Optional item icon atlas for textured icons.</param>
    public void Initialize(
        InventoryContainer inventory,
        int columns,
        ItemRegistry itemRegistry,
        ItemIconAtlas? iconAtlas = null)
    {
        Columns = columns;
        AddThemeConstantOverride("h_separation", SlotSeparation);
        AddThemeConstantOverride("v_separation", SlotSeparation);

        _slotSceneCache ??= GD.Load<PackedScene>(SlotScenePath);
        _slotNodes = new InventorySlotNode[inventory.SlotCount];

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            InventorySlotNode slotNode = _slotSceneCache.Instantiate<InventorySlotNode>();
            slotNode.Name = $"Slot{i}";
            AddChild(slotNode);
            slotNode.Initialize(inventory, i, itemRegistry, iconAtlas);

            slotNode.SlotClicked += OnSlotClicked;
            slotNode.SlotHovered += OnSlotHovered;
            slotNode.SlotUnhovered += OnSlotUnhovered;

            _slotNodes[i] = slotNode;
        }
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        foreach (InventorySlotNode slotNode in _slotNodes)
        {
            slotNode.SlotClicked -= OnSlotClicked;
            slotNode.SlotHovered -= OnSlotHovered;
            slotNode.SlotUnhovered -= OnSlotUnhovered;
        }
    }

    private void OnSlotClicked(object? sender, SlotClickedEventArgs e) => SlotClicked?.Invoke(this, e);

    private void OnSlotHovered(object? sender, SlotHoverEventArgs e) => SlotHovered?.Invoke(this, e);

    private void OnSlotUnhovered(object? sender, SlotHoverEventArgs e) => SlotUnhovered?.Invoke(this, e);
}
