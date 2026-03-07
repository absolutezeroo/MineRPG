using System;

using Godot;

using MineRPG.RPG.Inventory;
using MineRPG.RPG.Items;

using InventoryContainer = MineRPG.RPG.Inventory.Inventory;

namespace MineRPG.Godot.UI.Inventory;

/// <summary>
/// A vertical panel displaying 4 armor slots and 1 offhand slot.
/// Each slot shows a label ("Helmet", "Chest", etc.) when empty.
/// </summary>
public sealed partial class ArmorPanelNode : VBoxContainer
{
    private static readonly string[] ArmorSlotLabels = ["Helmet", "Chest", "Legs", "Boots"];

    private InventorySlotNode[] _armorSlots = [];
    private InventorySlotNode? _offhandSlot;

    /// <summary>Raised when any slot is clicked.</summary>
    public event EventHandler<SlotClickedEventArgs>? SlotClicked;

    /// <summary>Raised when the mouse enters any slot.</summary>
    public event EventHandler<SlotHoverEventArgs>? SlotHovered;

    /// <summary>Raised when the mouse exits any slot.</summary>
    public event EventHandler<SlotHoverEventArgs>? SlotUnhovered;

    /// <summary>
    /// Builds the armor panel with 4 armor slots and 1 offhand slot.
    /// </summary>
    /// <param name="armorInventory">The 4-slot armor inventory.</param>
    /// <param name="offhandInventory">The 1-slot offhand inventory.</param>
    /// <param name="itemRegistry">The item registry for definition lookups.</param>
    public void Initialize(
        InventoryContainer armorInventory,
        InventoryContainer offhandInventory,
        ItemRegistry itemRegistry)
    {
        AddThemeConstantOverride("separation", 4);

        _armorSlots = new InventorySlotNode[PlayerInventory.ArmorSlotCount];

        for (int i = 0; i < PlayerInventory.ArmorSlotCount; i++)
        {
            Label label = new();
            label.Text = ArmorSlotLabels[i];
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.ThemeTypeVariation = ThemeTypeVariations.CaptionLabel;
            AddChild(label);

            InventorySlotNode slotNode = new();
            slotNode.Name = $"Armor{i}";
            AddChild(slotNode);
            slotNode.Initialize(armorInventory, i, itemRegistry);

            slotNode.SlotClicked += OnSlotClicked;
            slotNode.SlotHovered += OnSlotHovered;
            slotNode.SlotUnhovered += OnSlotUnhovered;

            _armorSlots[i] = slotNode;
        }

        HSeparator separator = new();
        AddChild(separator);

        Label offhandLabel = new();
        offhandLabel.Text = "Offhand";
        offhandLabel.HorizontalAlignment = HorizontalAlignment.Center;
        offhandLabel.ThemeTypeVariation = ThemeTypeVariations.CaptionLabel;
        AddChild(offhandLabel);

        _offhandSlot = new InventorySlotNode();
        _offhandSlot.Name = "Offhand";
        AddChild(_offhandSlot);
        _offhandSlot.Initialize(offhandInventory, 0, itemRegistry);

        _offhandSlot.SlotClicked += OnSlotClicked;
        _offhandSlot.SlotHovered += OnSlotHovered;
        _offhandSlot.SlotUnhovered += OnSlotUnhovered;
    }

    /// <inheritdoc />
    public override void _ExitTree()
    {
        foreach (InventorySlotNode slotNode in _armorSlots)
        {
            slotNode.SlotClicked -= OnSlotClicked;
            slotNode.SlotHovered -= OnSlotHovered;
            slotNode.SlotUnhovered -= OnSlotUnhovered;
        }

        if (_offhandSlot != null)
        {
            _offhandSlot.SlotClicked -= OnSlotClicked;
            _offhandSlot.SlotHovered -= OnSlotHovered;
            _offhandSlot.SlotUnhovered -= OnSlotUnhovered;
        }
    }

    private void OnSlotClicked(object? sender, SlotClickedEventArgs e) => SlotClicked?.Invoke(this, e);

    private void OnSlotHovered(object? sender, SlotHoverEventArgs e) => SlotHovered?.Invoke(this, e);

    private void OnSlotUnhovered(object? sender, SlotHoverEventArgs e) => SlotUnhovered?.Invoke(this, e);
}
