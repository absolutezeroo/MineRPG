using System;

using Godot;

using MineRPG.RPG.Items;

namespace MineRPG.Godot.UI;

/// <summary>
/// Runtime color palette and style factories for the game UI.
/// The base theme is defined in <c>Resources/Themes/GameTheme.tres</c> and assigned
/// to root Controls via scene files. This class provides only runtime-dependent helpers:
/// color lookups, per-instance style factories, and constants used by code at runtime.
/// </summary>
public static class GameTheme
{
    // -------------------------------------------------------------------------
    // Color palette (runtime-referenced constants)
    // -------------------------------------------------------------------------

    /// <summary>Full-screen overlay darkening color (semi-transparent black).</summary>
    public static readonly Color Overlay = new(0.0f, 0.0f, 0.0f, 0.55f);

    /// <summary>Standard body text (light gray).</summary>
    public static readonly Color TextBody = new(0.85f, 0.85f, 0.85f, 1.0f);

    /// <summary>Content area background (darker panel interior).</summary>
    public static readonly Color ContentBackground = new(0.14f, 0.12f, 0.10f, 0.85f);

    // -------------------------------------------------------------------------
    // Item rarity colors
    // -------------------------------------------------------------------------

    /// <summary>Common item rarity color (white).</summary>
    public static readonly Color RarityCommon = new(1.0f, 1.0f, 1.0f, 1.0f);

    /// <summary>Uncommon item rarity color (green).</summary>
    public static readonly Color RarityUncommon = new(0.33f, 0.80f, 0.33f, 1.0f);

    /// <summary>Rare item rarity color (blue).</summary>
    public static readonly Color RarityRare = new(0.33f, 0.55f, 1.0f, 1.0f);

    /// <summary>Epic item rarity color (purple).</summary>
    public static readonly Color RarityEpic = new(0.70f, 0.33f, 1.0f, 1.0f);

    /// <summary>Legendary item rarity color (orange/gold).</summary>
    public static readonly Color RarityLegendary = new(1.0f, 0.65f, 0.0f, 1.0f);

    // -------------------------------------------------------------------------
    // Item category placeholder colors
    // -------------------------------------------------------------------------

    /// <summary>Placeholder color for block items.</summary>
    public static readonly Color CategoryBlock = new(0.55f, 0.45f, 0.35f, 0.85f);

    /// <summary>Placeholder color for tool items.</summary>
    public static readonly Color CategoryTool = new(0.50f, 0.60f, 0.70f, 0.85f);

    /// <summary>Placeholder color for weapon items.</summary>
    public static readonly Color CategoryWeapon = new(0.70f, 0.35f, 0.35f, 0.85f);

    /// <summary>Placeholder color for armor items.</summary>
    public static readonly Color CategoryArmor = new(0.45f, 0.55f, 0.65f, 0.85f);

    /// <summary>Placeholder color for consumable items.</summary>
    public static readonly Color CategoryConsumable = new(0.65f, 0.40f, 0.50f, 0.85f);

    /// <summary>Placeholder color for material items.</summary>
    public static readonly Color CategoryMaterial = new(0.55f, 0.55f, 0.40f, 0.85f);

    /// <summary>Placeholder color for miscellaneous items.</summary>
    public static readonly Color CategoryMisc = new(0.50f, 0.50f, 0.50f, 0.85f);

    // -------------------------------------------------------------------------
    // Inventory slot colors
    // -------------------------------------------------------------------------

    /// <summary>Inventory slot hover border color.</summary>
    public static readonly Color SlotHoverBorder = new(0.80f, 0.80f, 0.70f, 0.95f);

    /// <summary>Tooltip panel background (dark, nearly opaque).</summary>
    public static readonly Color TooltipBackground = new(0.10f, 0.08f, 0.06f, 0.95f);

    /// <summary>Tooltip panel border color.</summary>
    public static readonly Color TooltipBorder = new(0.35f, 0.30f, 0.25f, 1.0f);

    /// <summary>Hotbar slot background color.</summary>
    public static readonly Color SlotBackground = new(0.15f, 0.15f, 0.15f, 0.75f);

    /// <summary>Hotbar slot border color (unselected).</summary>
    public static readonly Color SlotBorder = new(0.50f, 0.50f, 0.50f, 0.85f);

    /// <summary>Hotbar slot border color (selected).</summary>
    public static readonly Color SlotSelectedBorder = new(1.0f, 1.0f, 1.0f, 0.95f);

    // -------------------------------------------------------------------------
    // Controls tab rebind colors
    // -------------------------------------------------------------------------

    /// <summary>Rebind button normal background.</summary>
    public static readonly Color RebindNormal = new(0.28f, 0.24f, 0.20f, 1.0f);

    /// <summary>Rebind button active-listening background (yellow tint).</summary>
    public static readonly Color RebindListening = new(0.45f, 0.38f, 0.10f, 1.0f);

    /// <summary>Rebind button border color.</summary>
    public static readonly Color RebindBorder = new(0.40f, 0.35f, 0.28f, 1.0f);

    /// <summary>Text color while a rebind button is in listening state (yellow).</summary>
    public static readonly Color ListeningText = new(1.0f, 0.85f, 0.30f, 1.0f);

    // -------------------------------------------------------------------------
    // World selection colors
    // -------------------------------------------------------------------------

    /// <summary>World list entry normal background.</summary>
    public static readonly Color WorldEntryNormal = new(0.25f, 0.22f, 0.18f, 0.85f);

    /// <summary>World list entry hover background.</summary>
    public static readonly Color WorldEntryHover = new(0.35f, 0.30f, 0.25f, 0.90f);

    /// <summary>World list entry border color.</summary>
    public static readonly Color WorldEntryBorder = new(0.30f, 0.25f, 0.20f, 0.80f);

    /// <summary>World list entry hover border color.</summary>
    public static readonly Color WorldEntryHoverBorder = new(0.50f, 0.45f, 0.35f, 0.90f);

    // -------------------------------------------------------------------------
    // Layout constants
    // -------------------------------------------------------------------------

    /// <summary>Standard border width for panels and buttons (px).</summary>
    public const int BorderWidth = 2;

    /// <summary>Thin border width for inner content areas and list items (px).</summary>
    public const int BorderWidthThin = 1;

    // -------------------------------------------------------------------------
    // Tab style colors
    // -------------------------------------------------------------------------

    /// <summary>Active tab button background.</summary>
    public static readonly Color TabActive = new(0.35f, 0.30f, 0.22f, 1.0f);

    /// <summary>Inactive tab button background.</summary>
    public static readonly Color TabInactive = new(0.22f, 0.19f, 0.15f, 1.0f);

    /// <summary>Tab strip border color.</summary>
    public static readonly Color TabBorder = new(0.45f, 0.38f, 0.28f, 1.0f);

    // -------------------------------------------------------------------------
    // Rarity color lookup
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the display color for the given item rarity tier.
    /// </summary>
    /// <param name="rarity">The item rarity.</param>
    /// <returns>The corresponding UI color.</returns>
    public static Color GetRarityColor(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Common => RarityCommon,
            ItemRarity.Uncommon => RarityUncommon,
            ItemRarity.Rare => RarityRare,
            ItemRarity.Epic => RarityEpic,
            ItemRarity.Legendary => RarityLegendary,
            _ => throw new ArgumentOutOfRangeException(
                nameof(rarity), rarity, "Unhandled item rarity"),
        };
    }

    /// <summary>
    /// Returns the placeholder icon color for the given item category.
    /// Used until real item icon textures are available.
    /// </summary>
    /// <param name="category">The item category.</param>
    /// <returns>The corresponding placeholder color.</returns>
    public static Color GetCategoryPlaceholderColor(ItemCategory category)
    {
        return category switch
        {
            ItemCategory.Block => CategoryBlock,
            ItemCategory.Tool => CategoryTool,
            ItemCategory.Weapon => CategoryWeapon,
            ItemCategory.Armor => CategoryArmor,
            ItemCategory.Consumable => CategoryConsumable,
            ItemCategory.Material => CategoryMaterial,
            ItemCategory.Misc => CategoryMisc,
            _ => CategoryMisc,
        };
    }

    // -------------------------------------------------------------------------
    // Per-instance style factories (for runtime state changes)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a StyleBoxFlat for a tab button in active or inactive state.
    /// </summary>
    /// <param name="isActive">True for the selected/active tab.</param>
    /// <returns>A configured StyleBoxFlat with the appropriate tab colors.</returns>
    public static StyleBoxFlat CreateTabStyle(bool isActive)
    {
        StyleBoxFlat style = new();
        style.BgColor = isActive ? TabActive : TabInactive;
        style.SetBorderWidthAll(BorderWidthThin);
        style.BorderColor = TabBorder;
        style.SetContentMarginAll(6f);
        return style;
    }

    /// <summary>
    /// Creates a StyleBoxFlat for a rebind button in normal or listening state.
    /// </summary>
    /// <param name="isListening">True if the button is currently waiting for a key press.</param>
    /// <returns>A configured StyleBoxFlat.</returns>
    public static StyleBoxFlat CreateRebindButtonStyle(bool isListening)
    {
        StyleBoxFlat style = new();
        style.BgColor = isListening ? RebindListening : RebindNormal;
        style.SetBorderWidthAll(BorderWidthThin);
        style.BorderColor = RebindBorder;
        style.SetContentMarginAll(4f);
        return style;
    }

    /// <summary>
    /// Creates a StyleBoxFlat for a world list entry button in normal state.
    /// </summary>
    /// <returns>A configured StyleBoxFlat for world list entries.</returns>
    public static StyleBoxFlat CreateWorldEntryStyle()
    {
        StyleBoxFlat style = new();
        style.BgColor = WorldEntryNormal;
        style.SetBorderWidthAll(BorderWidthThin);
        style.BorderColor = WorldEntryBorder;
        style.SetContentMarginAll(6f);
        return style;
    }

    /// <summary>
    /// Creates a StyleBoxFlat for a world list entry button in hover state.
    /// </summary>
    /// <returns>A configured StyleBoxFlat for world list entry hover.</returns>
    public static StyleBoxFlat CreateWorldEntryHoverStyle()
    {
        StyleBoxFlat style = new();
        style.BgColor = WorldEntryHover;
        style.SetBorderWidthAll(BorderWidthThin);
        style.BorderColor = WorldEntryHoverBorder;
        style.SetContentMarginAll(6f);
        return style;
    }
}
