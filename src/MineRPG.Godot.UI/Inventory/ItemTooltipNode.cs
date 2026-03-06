using Godot;

using MineRPG.RPG.Items;

namespace MineRPG.Godot.UI.Inventory;

/// <summary>
/// A tooltip popup that displays item information when hovering over a slot.
/// Shows: name (colored by rarity), category, description, and tool/weapon/armor stats.
/// Positioned near the mouse, clamped to the viewport.
/// </summary>
public sealed partial class ItemTooltipNode : PanelContainer
{
    private const int TooltipMargin = 12;

    private Label _nameLabel = null!;
    private Label _categoryLabel = null!;
    private Label _descriptionLabel = null!;
    private Label _statsLabel = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        BuildUI();
        Visible = false;
    }

    /// <summary>
    /// Shows the tooltip for the given item definition at the current mouse position.
    /// </summary>
    /// <param name="definition">The item definition to display.</param>
    /// <param name="item">The item instance for count/durability info.</param>
    public void ShowForItem(ItemDefinition definition, ItemInstance item)
    {
        _nameLabel.Text = definition.DisplayName;
        _nameLabel.AddThemeColorOverride("font_color", GameTheme.GetRarityColor(definition.Rarity));

        _categoryLabel.Text = definition.Category.ToString();

        if (!string.IsNullOrEmpty(definition.Description))
        {
            _descriptionLabel.Text = definition.Description;
            _descriptionLabel.Visible = true;
        }
        else
        {
            _descriptionLabel.Visible = false;
        }

        string statsText = BuildStatsText(definition, item);

        if (!string.IsNullOrEmpty(statsText))
        {
            _statsLabel.Text = statsText;
            _statsLabel.Visible = true;
        }
        else
        {
            _statsLabel.Visible = false;
        }

        Visible = true;
        Callable.From(PositionTooltip).CallDeferred();
    }

    /// <summary>
    /// Hides the tooltip.
    /// </summary>
    public void HideTooltip() => Visible = false;

    private void BuildUI()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(160, 0);
        ZIndex = 101;

        StyleBoxFlat tooltipStyle = new();
        tooltipStyle.BgColor = GameTheme.TooltipBackground;
        tooltipStyle.SetBorderWidthAll(GameTheme.BorderWidthThin);
        tooltipStyle.BorderColor = GameTheme.TooltipBorder;
        tooltipStyle.SetContentMarginAll(8f);
        AddThemeStyleboxOverride("panel", tooltipStyle);

        VBoxContainer vbox = new();
        vbox.AddThemeConstantOverride("separation", 4);
        vbox.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(vbox);

        _nameLabel = new Label();
        _nameLabel.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeButton);
        _nameLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(_nameLabel);

        _categoryLabel = new Label();
        _categoryLabel.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeSmall);
        _categoryLabel.AddThemeColorOverride("font_color", GameTheme.TextSub);
        _categoryLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(_categoryLabel);

        HSeparator separator = new();
        vbox.AddChild(separator);

        _descriptionLabel = new Label();
        _descriptionLabel.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeSmall);
        _descriptionLabel.AddThemeColorOverride("font_color", GameTheme.TextBody);
        _descriptionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _descriptionLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(_descriptionLabel);

        _statsLabel = new Label();
        _statsLabel.AddThemeFontSizeOverride("font_size", GameTheme.FontSizeSmall);
        _statsLabel.AddThemeColorOverride("font_color", GameTheme.AccentGreen);
        _statsLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _statsLabel.MouseFilter = MouseFilterEnum.Ignore;
        vbox.AddChild(_statsLabel);
    }

    private void PositionTooltip()
    {
        Vector2 mousePos = GetGlobalMousePosition();
        Vector2 viewportSize = GetViewportRect().Size;
        Vector2 tooltipSize = Size;

        float posX = mousePos.X + TooltipMargin;
        float posY = mousePos.Y + TooltipMargin;

        if (posX + tooltipSize.X > viewportSize.X)
        {
            posX = mousePos.X - tooltipSize.X - TooltipMargin;
        }

        if (posY + tooltipSize.Y > viewportSize.Y)
        {
            posY = mousePos.Y - tooltipSize.Y - TooltipMargin;
        }

        GlobalPosition = new Vector2(
            Mathf.Max(0, posX),
            Mathf.Max(0, posY));
    }

    private static string BuildStatsText(ItemDefinition definition, ItemInstance item)
    {
        System.Text.StringBuilder stats = new();

        if (definition.Tool != null)
        {
            stats.AppendLine($"Mining Speed: {definition.Tool.MiningSpeed:F1}");
            stats.AppendLine($"Harvest Level: {definition.Tool.HarvestLevel}");
        }

        if (definition.Weapon != null)
        {
            stats.AppendLine($"Damage: {definition.Weapon.BaseDamage:F1}");
            stats.AppendLine($"Attack Speed: {definition.Weapon.AttackSpeed:F1}");
        }

        if (definition.Armor != null)
        {
            stats.AppendLine($"Defense: {definition.Armor.Defense:F1}");
            stats.AppendLine($"Toughness: {definition.Armor.Toughness:F1}");
        }

        if (definition.Consumable != null)
        {
            if (definition.Consumable.HealthRestore > 0)
            {
                stats.AppendLine($"Restores {definition.Consumable.HealthRestore:F0} HP");
            }

            if (definition.Consumable.HungerRestore > 0)
            {
                stats.AppendLine($"Restores {definition.Consumable.HungerRestore:F0} Hunger");
            }
        }

        if (item.HasDurability)
        {
            stats.AppendLine($"Durability: {item.CurrentDurability}/{definition.MaxDurability}");
        }

        if (definition.MaxStackSize > 1)
        {
            stats.AppendLine($"Max Stack: {definition.MaxStackSize}");
        }

        return stats.ToString().TrimEnd();
    }
}
