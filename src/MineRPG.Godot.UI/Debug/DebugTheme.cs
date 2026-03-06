#if DEBUG
using Godot;

namespace MineRPG.Godot.UI.Debug;

/// <summary>
/// Single source of truth for all debug UI colors, font sizes, and panel styles.
/// Methods return pre-configured Godot style objects for consistent debug UI appearance.
/// Dark, compact, monospace - no animations, instant response.
/// </summary>
public static class DebugTheme
{
    // -- Colors --

    /// <summary>Dark semi-transparent panel background.</summary>
    public static readonly Color PanelBackground = new(0.05f, 0.05f, 0.08f, 0.85f);

    /// <summary>Primary text color (white).</summary>
    public static readonly Color TextPrimary = new(1f, 1f, 1f, 1f);

    /// <summary>Secondary text color (dim gray).</summary>
    public static readonly Color TextSecondary = new(0.7f, 0.7f, 0.7f, 1f);

    /// <summary>Section header text color (green accent).</summary>
    public static readonly Color TextAccent = new(0.55f, 0.85f, 0.55f, 1f);

    /// <summary>Warning text color (yellow).</summary>
    public static readonly Color TextWarning = new(1f, 0.85f, 0.3f, 1f);

    /// <summary>Error/critical text color (red).</summary>
    public static readonly Color TextError = new(1f, 0.4f, 0.4f, 1f);

    /// <summary>Good value color (green).</summary>
    public static readonly Color ValueGood = new(0.4f, 0.9f, 0.4f, 1f);

    /// <summary>Neutral value color (white).</summary>
    public static readonly Color ValueNeutral = new(1f, 1f, 1f, 1f);

    /// <summary>Default graph line color.</summary>
    public static readonly Color GraphLine = new(0.4f, 0.9f, 0.4f, 1f);

    /// <summary>Graph spike marker color.</summary>
    public static readonly Color GraphSpike = new(1f, 0.3f, 0.3f, 1f);

    /// <summary>Graph target line color (e.g., 60 FPS line).</summary>
    public static readonly Color GraphTarget = new(1f, 0.3f, 0.3f, 0.6f);

    /// <summary>Graph background color.</summary>
    public static readonly Color GraphBackground = new(0.05f, 0.05f, 0.08f, 0.7f);

    /// <summary>Text shadow color.</summary>
    public static readonly Color ShadowColor = new(0f, 0f, 0f, 0.75f);

    /// <summary>Active tab button background.</summary>
    public static readonly Color TabActiveColor = new(0.2f, 0.3f, 0.4f, 0.9f);

    /// <summary>Inactive tab button background.</summary>
    public static readonly Color TabInactiveColor = new(0.1f, 0.1f, 0.15f, 0.7f);

    /// <summary>Slider filled portion color.</summary>
    public static readonly Color SliderFillColor = new(0.3f, 0.6f, 0.9f, 0.8f);

    /// <summary>Toggle enabled color.</summary>
    public static readonly Color ToggleOnColor = new(0.4f, 0.8f, 0.4f, 1f);

    /// <summary>Toggle disabled color.</summary>
    public static readonly Color ToggleOffColor = new(0.5f, 0.5f, 0.5f, 1f);

    // -- Font sizes --

    /// <summary>Small font size (12px).</summary>
    public const int FontSizeSmall = 12;

    /// <summary>Normal font size (14px).</summary>
    public const int FontSizeNormal = 14;

    /// <summary>Header font size (16px).</summary>
    public const int FontSizeHeader = 16;

    /// <summary>Title font size (18px).</summary>
    public const int FontSizeTitle = 18;

    // -- Spacing --

    /// <summary>Horizontal padding inside panels.</summary>
    public const float PanelPaddingX = 8f;

    /// <summary>Vertical padding inside panels.</summary>
    public const float PanelPaddingY = 6f;

    /// <summary>Spacing between sections.</summary>
    public const float SectionSpacing = 8f;

    /// <summary>Shadow offset in pixels.</summary>
    public const int ShadowOffset = 1;

    /// <summary>Debug menu panel width.</summary>
    public const float MenuPanelWidth = 420f;

    /// <summary>Graph default width.</summary>
    public const float GraphWidth = 400f;

    /// <summary>Graph default height.</summary>
    public const float GraphHeight = 120f;

    /// <summary>Chunk map default size.</summary>
    public const float ChunkMapSize = 300f;

    // -- Panel styles --

    /// <summary>
    /// Creates a dark semi-transparent panel style box.
    /// </summary>
    /// <returns>A configured StyleBoxFlat for debug panels.</returns>
    public static StyleBoxFlat CreatePanelStyle()
    {
        StyleBoxFlat style = new();
        style.BgColor = PanelBackground;
        style.ContentMarginLeft = PanelPaddingX;
        style.ContentMarginRight = PanelPaddingX;
        style.ContentMarginTop = PanelPaddingY;
        style.ContentMarginBottom = PanelPaddingY;
        style.CornerRadiusTopLeft = 3;
        style.CornerRadiusTopRight = 3;
        style.CornerRadiusBottomLeft = 3;
        style.CornerRadiusBottomRight = 3;
        return style;
    }

    /// <summary>
    /// Creates a style box for the active tab button.
    /// </summary>
    /// <returns>A configured StyleBoxFlat for active tabs.</returns>
    public static StyleBoxFlat CreateTabActiveStyle()
    {
        StyleBoxFlat style = new();
        style.BgColor = TabActiveColor;
        style.ContentMarginLeft = 6f;
        style.ContentMarginRight = 6f;
        style.ContentMarginTop = 4f;
        style.ContentMarginBottom = 4f;
        style.CornerRadiusTopLeft = 3;
        style.CornerRadiusTopRight = 3;
        return style;
    }

    /// <summary>
    /// Creates a style box for inactive tab buttons.
    /// </summary>
    /// <returns>A configured StyleBoxFlat for inactive tabs.</returns>
    public static StyleBoxFlat CreateTabInactiveStyle()
    {
        StyleBoxFlat style = new();
        style.BgColor = TabInactiveColor;
        style.ContentMarginLeft = 6f;
        style.ContentMarginRight = 6f;
        style.ContentMarginTop = 4f;
        style.ContentMarginBottom = 4f;
        style.CornerRadiusTopLeft = 3;
        style.CornerRadiusTopRight = 3;
        return style;
    }

    /// <summary>
    /// Creates a styled button box with consistent padding and rounded corners.
    /// </summary>
    /// <param name="bgColor">Background color for the button state.</param>
    /// <returns>A configured StyleBoxFlat.</returns>
    public static StyleBoxFlat CreateButtonStyle(Color bgColor)
    {
        StyleBoxFlat style = new();
        style.BgColor = bgColor;
        style.ContentMarginLeft = 8f;
        style.ContentMarginRight = 8f;
        style.ContentMarginTop = 4f;
        style.ContentMarginBottom = 4f;
        style.CornerRadiusTopLeft = 3;
        style.CornerRadiusTopRight = 3;
        style.CornerRadiusBottomLeft = 3;
        style.CornerRadiusBottomRight = 3;
        return style;
    }

    /// <summary>
    /// Applies standard debug label styling to an existing Label.
    /// </summary>
    /// <param name="label">The label to style.</param>
    /// <param name="color">Text color.</param>
    /// <param name="fontSize">Font size.</param>
    public static void ApplyLabelStyle(Label label, Color color, int fontSize = FontSizeNormal)
    {
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_shadow_color", ShadowColor);
        label.AddThemeConstantOverride("shadow_offset_x", ShadowOffset);
        label.AddThemeConstantOverride("shadow_offset_y", ShadowOffset);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    /// <summary>
    /// Returns a color indicating value quality (green=good, yellow=attention, red=problem).
    /// </summary>
    /// <param name="value">The value to evaluate.</param>
    /// <param name="goodThreshold">Below this is good.</param>
    /// <param name="warnThreshold">Below this is warning, above is error.</param>
    /// <returns>The appropriate color.</returns>
    public static Color GetValueColor(double value, double goodThreshold, double warnThreshold)
    {
        if (value <= goodThreshold)
        {
            return ValueGood;
        }

        if (value <= warnThreshold)
        {
            return TextWarning;
        }

        return TextError;
    }
}
#endif
