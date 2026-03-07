namespace MineRPG.Godot.UI;

/// <summary>
/// String constants for Godot Theme Type Variation names used throughout the UI layer.
/// Avoids magic strings when assigning <c>ThemeTypeVariation</c> on Controls.
/// </summary>
public static class ThemeTypeVariations
{
    /// <summary>Main menu hero title (56px, white).</summary>
    public const string HeroLabel = "HeroLabel";

    /// <summary>Main menu title shadow (56px, shadow color).</summary>
    public const string HeroShadowLabel = "HeroShadowLabel";

    /// <summary>Pause menu screen title (32px, white).</summary>
    public const string ScreenTitleLabel = "ScreenTitleLabel";

    /// <summary>Options/WorldSelection/Inventory panel titles (26px, white).</summary>
    public const string PanelTitleLabel = "PanelTitleLabel";

    /// <summary>Loading screen title (36px, white).</summary>
    public const string LoadingTitleLabel = "LoadingTitleLabel";

    /// <summary>Options section headers (14px, accent green).</summary>
    public const string SectionHeaderLabel = "SectionHeaderLabel";

    /// <summary>Armor slot labels, tooltip category (14px, subdued).</summary>
    public const string CaptionLabel = "CaptionLabel";

    /// <summary>Hotbar label, loading status, empty world msg (16px, subdued).</summary>
    public const string SubduedBodyLabel = "SubduedBodyLabel";

    /// <summary>Slot item count overlay (14px, white).</summary>
    public const string SlotCountLabel = "SlotCountLabel";

    /// <summary>Tooltip item name (18px, color set dynamically per rarity).</summary>
    public const string TooltipNameLabel = "TooltipNameLabel";

    /// <summary>Tooltip description text (14px, body color).</summary>
    public const string TooltipDescriptionLabel = "TooltipDescriptionLabel";

    /// <summary>Tooltip stat lines (14px, accent green).</summary>
    public const string TooltipStatsLabel = "TooltipStatsLabel";

    /// <summary>Main menu version label (14px, version color).</summary>
    public const string VersionLabel = "VersionLabel";

    /// <summary>Large menu buttons (20px font).</summary>
    public const string LargeMenuButton = "LargeMenuButton";
}
