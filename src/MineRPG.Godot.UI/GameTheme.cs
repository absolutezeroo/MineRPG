using System;

using Godot;

namespace MineRPG.Godot.UI;

/// <summary>
/// Single source of truth for the game UI palette, font sizes, and Godot Theme.
/// Call <see cref="Initialize"/> once at startup (in GameBootstrapper._Ready) before
/// any UI nodes are created. Assign the theme to root Control nodes via
/// <see cref="Apply"/> inside each node's _Ready method.
/// </summary>
public static class GameTheme
{
    // -------------------------------------------------------------------------
    // Color palette
    // -------------------------------------------------------------------------

    /// <summary>Dark brownish-black panel background (semi-transparent).</summary>
    public static readonly Color BackgroundDark = new(0.18f, 0.15f, 0.12f, 0.95f);

    /// <summary>Full-screen overlay darkening color (semi-transparent black).</summary>
    public static readonly Color Overlay = new(0.0f, 0.0f, 0.0f, 0.55f);

    /// <summary>Main menu dirt-style background color.</summary>
    public static readonly Color BackgroundMenu = new(0.22f, 0.17f, 0.13f, 1.0f);

    /// <summary>Loading screen background color (near-black).</summary>
    public static readonly Color BackgroundLoading = new(0.08f, 0.08f, 0.08f, 1.0f);

    /// <summary>Button normal state background.</summary>
    public static readonly Color ButtonNormal = new(0.35f, 0.35f, 0.35f, 0.85f);

    /// <summary>Button hover state background.</summary>
    public static readonly Color ButtonHover = new(0.45f, 0.45f, 0.50f, 0.90f);

    /// <summary>Button pressed state background.</summary>
    public static readonly Color ButtonPressed = new(0.30f, 0.30f, 0.35f, 0.95f);

    /// <summary>Button disabled state background.</summary>
    public static readonly Color ButtonDisabled = new(0.22f, 0.22f, 0.22f, 0.60f);

    /// <summary>Button border color.</summary>
    public static readonly Color ButtonBorder = new(0.20f, 0.20f, 0.20f, 1.0f);

    /// <summary>Panel border color (warm brownish).</summary>
    public static readonly Color PanelBorder = new(0.30f, 0.25f, 0.20f, 1.0f);

    /// <summary>Primary title/heading text (white).</summary>
    public static readonly Color TextTitle = new(1.0f, 1.0f, 1.0f, 1.0f);

    /// <summary>Standard body text (light gray).</summary>
    public static readonly Color TextBody = new(0.85f, 0.85f, 0.85f, 1.0f);

    /// <summary>Subdued secondary text (warm gray).</summary>
    public static readonly Color TextSub = new(0.70f, 0.70f, 0.65f, 1.0f);

    /// <summary>Version label text color.</summary>
    public static readonly Color TextVersion = new(0.60f, 0.60f, 0.60f, 0.80f);

    /// <summary>Title text shadow color.</summary>
    public static readonly Color TitleShadow = new(0.15f, 0.10f, 0.05f, 0.80f);

    /// <summary>Accent green for section headers and highlights.</summary>
    public static readonly Color AccentGreen = new(0.60f, 0.75f, 0.60f, 1.0f);

    /// <summary>Progress bar fill color (bright green).</summary>
    public static readonly Color ProgressFill = new(0.25f, 0.65f, 0.35f, 1.0f);

    /// <summary>Selected element highlight (near-white).</summary>
    public static readonly Color SelectedHighlight = new(1.0f, 1.0f, 1.0f, 0.95f);

    /// <summary>Active tab button background.</summary>
    public static readonly Color TabActive = new(0.35f, 0.30f, 0.22f, 1.0f);

    /// <summary>Inactive tab button background.</summary>
    public static readonly Color TabInactive = new(0.22f, 0.19f, 0.15f, 1.0f);

    /// <summary>Tab strip border color.</summary>
    public static readonly Color TabBorder = new(0.45f, 0.38f, 0.28f, 1.0f);

    /// <summary>Content area background (darker panel interior).</summary>
    public static readonly Color ContentBackground = new(0.14f, 0.12f, 0.10f, 0.85f);

    // Hotbar-specific (used in _Draw() directly — cannot go through Theme)

    /// <summary>Hotbar slot background color.</summary>
    public static readonly Color SlotBackground = new(0.15f, 0.15f, 0.15f, 0.75f);

    /// <summary>Hotbar slot border color (unselected).</summary>
    public static readonly Color SlotBorder = new(0.50f, 0.50f, 0.50f, 0.85f);

    /// <summary>Hotbar slot border color (selected).</summary>
    public static readonly Color SlotSelectedBorder = new(1.0f, 1.0f, 1.0f, 0.95f);

    // Controls tab rebind colors

    /// <summary>Rebind button normal background.</summary>
    public static readonly Color RebindNormal = new(0.28f, 0.24f, 0.20f, 1.0f);

    /// <summary>Rebind button active-listening background (yellow tint).</summary>
    public static readonly Color RebindListening = new(0.45f, 0.38f, 0.10f, 1.0f);

    /// <summary>Rebind button border color.</summary>
    public static readonly Color RebindBorder = new(0.40f, 0.35f, 0.28f, 1.0f);

    /// <summary>Text color while a rebind button is in listening state (yellow).</summary>
    public static readonly Color ListeningText = new(1.0f, 0.85f, 0.30f, 1.0f);

    // World selection

    /// <summary>World list entry normal background.</summary>
    public static readonly Color WorldEntryNormal = new(0.25f, 0.22f, 0.18f, 0.85f);

    /// <summary>World list entry hover background.</summary>
    public static readonly Color WorldEntryHover = new(0.35f, 0.30f, 0.25f, 0.90f);

    /// <summary>World list entry border color.</summary>
    public static readonly Color WorldEntryBorder = new(0.30f, 0.25f, 0.20f, 0.80f);

    /// <summary>World list entry hover border color.</summary>
    public static readonly Color WorldEntryHoverBorder = new(0.50f, 0.45f, 0.35f, 0.90f);

    // Font sizes

    /// <summary>Small sub-label text (14px).</summary>
    public const int FontSizeSmall = 14;

    /// <summary>Body and row label text (16px).</summary>
    public const int FontSizeBody = 16;

    /// <summary>Standard button text (18px).</summary>
    public const int FontSizeButton = 18;

    /// <summary>Menu button text (20px).</summary>
    public const int FontSizeButtonLarge = 20;

    /// <summary>Sub-panel header text (26px).</summary>
    public const int FontSizeSubTitle = 26;

    /// <summary>Panel title text (32px).</summary>
    public const int FontSizeTitle = 32;

    /// <summary>Loading screen title text (36px).</summary>
    public const int FontSizeTitleLarge = 36;

    /// <summary>Main menu hero title text (56px).</summary>
    public const int FontSizeHero = 56;

    // -------------------------------------------------------------------------
    // Layout constants
    // -------------------------------------------------------------------------

    /// <summary>Standard content margin inside panels (px).</summary>
    public const float PanelMargin = 16f;

    /// <summary>Standard border width for panels and buttons (px).</summary>
    public const int BorderWidth = 2;

    /// <summary>Thin border width for inner content areas and list items (px).</summary>
    public const int BorderWidthThin = 1;

    // -------------------------------------------------------------------------
    // Theme instance
    // -------------------------------------------------------------------------

    private static Theme? _instance;

    /// <summary>
    /// The built Godot Theme instance. Null until <see cref="Initialize"/> is called.
    /// </summary>
    /// <exception cref="InvalidOperationException">If accessed before Initialize() is called.</exception>
    public static Theme Instance
    {
        get
        {
            if (_instance is null)
            {
                throw new InvalidOperationException(
                    "GameTheme.Initialize() must be called before accessing Instance.");
            }

            return _instance;
        }
    }

    /// <summary>
    /// Builds the Godot Theme object from the palette constants.
    /// Must be called once from GameBootstrapper._Ready() before any UI nodes are created.
    /// </summary>
    public static void Initialize()
    {
        Theme theme = new();

        ApplyButtonStyles(theme);
        ApplyLabelStyles(theme);
        ApplyPanelContainerStyles(theme);
        ApplyProgressBarStyles(theme);
        ApplyHSliderStyles(theme);
        ApplyCheckButtonStyles(theme);
        ApplyOptionButtonStyles(theme);
        ApplyLineEditStyles(theme);
        ApplyHSeparatorStyles(theme);

        _instance = theme;
    }

    /// <summary>
    /// Assigns the built theme to a root Control node.
    /// All child Controls in the subtree inherit this theme automatically.
    /// Must be called after <see cref="Initialize"/>.
    /// </summary>
    /// <param name="rootControl">The root Control node to apply the theme to.</param>
    public static void Apply(Control rootControl) => rootControl.Theme = Instance;

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

    // -------------------------------------------------------------------------
    // Theme section builders (private)
    // -------------------------------------------------------------------------

    private static void ApplyButtonStyles(Theme theme)
    {
        StyleBoxFlat normalStyle = CreateButtonStyleBox(ButtonNormal);
        StyleBoxFlat hoverStyle = CreateButtonStyleBox(ButtonHover);
        StyleBoxFlat pressedStyle = CreateButtonStyleBox(ButtonPressed);
        StyleBoxFlat disabledStyle = CreateButtonStyleBox(ButtonDisabled);

        // Focus uses the hover color with a brighter border
        StyleBoxFlat focusStyle = CreateButtonStyleBox(ButtonHover);
        focusStyle.BorderColor = SelectedHighlight;

        theme.SetStylebox("normal", "Button", normalStyle);
        theme.SetStylebox("hover", "Button", hoverStyle);
        theme.SetStylebox("pressed", "Button", pressedStyle);
        theme.SetStylebox("disabled", "Button", disabledStyle);
        theme.SetStylebox("focus", "Button", focusStyle);

        theme.SetColor("font_color", "Button", TextBody);
        theme.SetColor("font_hover_color", "Button", TextTitle);
        theme.SetColor("font_pressed_color", "Button", TextTitle);
        theme.SetColor("font_disabled_color", "Button", TextSub);

        theme.SetFontSize("font_size", "Button", FontSizeButton);
    }

    private static void ApplyLabelStyles(Theme theme)
    {
        theme.SetColor("font_color", "Label", TextBody);
        theme.SetFontSize("font_size", "Label", FontSizeBody);
    }

    private static void ApplyPanelContainerStyles(Theme theme)
    {
        StyleBoxFlat panelStyle = new();
        panelStyle.BgColor = BackgroundDark;
        panelStyle.SetBorderWidthAll(BorderWidth);
        panelStyle.BorderColor = PanelBorder;
        panelStyle.SetContentMarginAll(PanelMargin);

        theme.SetStylebox("panel", "PanelContainer", panelStyle);
    }

    private static void ApplyProgressBarStyles(Theme theme)
    {
        StyleBoxFlat backgroundStyle = new();
        backgroundStyle.BgColor = new Color(0.20f, 0.20f, 0.20f, 1.0f);
        backgroundStyle.SetBorderWidthAll(BorderWidthThin);
        backgroundStyle.BorderColor = new Color(0.35f, 0.35f, 0.35f, 1.0f);

        StyleBoxFlat fillStyle = new();
        fillStyle.BgColor = ProgressFill;

        theme.SetStylebox("background", "ProgressBar", backgroundStyle);
        theme.SetStylebox("fill", "ProgressBar", fillStyle);

        theme.SetColor("font_color", "ProgressBar", TextBody);
        theme.SetFontSize("font_size", "ProgressBar", FontSizeSmall);
    }

    private static void ApplyHSliderStyles(Theme theme)
    {
        StyleBoxFlat sliderStyle = new();
        sliderStyle.BgColor = new Color(0.20f, 0.20f, 0.20f, 0.90f);
        sliderStyle.SetBorderWidthAll(BorderWidthThin);
        sliderStyle.BorderColor = ButtonBorder;
        sliderStyle.ContentMarginTop = 4f;
        sliderStyle.ContentMarginBottom = 4f;

        StyleBoxFlat grabberAreaStyle = new();
        grabberAreaStyle.BgColor = AccentGreen;
        grabberAreaStyle.ContentMarginTop = 4f;
        grabberAreaStyle.ContentMarginBottom = 4f;

        StyleBoxFlat grabberHighlightStyle = new();
        grabberHighlightStyle.BgColor = ProgressFill;
        grabberHighlightStyle.ContentMarginTop = 4f;
        grabberHighlightStyle.ContentMarginBottom = 4f;

        theme.SetStylebox("slider", "HSlider", sliderStyle);
        theme.SetStylebox("grabber_area", "HSlider", grabberAreaStyle);
        theme.SetStylebox("grabber_area_highlight", "HSlider", grabberHighlightStyle);
    }

    private static void ApplyCheckButtonStyles(Theme theme)
    {
        theme.SetStylebox("normal", "CheckButton", CreateButtonStyleBox(ButtonNormal));
        theme.SetStylebox("pressed", "CheckButton", CreateButtonStyleBox(ButtonPressed));
        theme.SetStylebox("hover", "CheckButton", CreateButtonStyleBox(ButtonHover));
        theme.SetStylebox("hover_pressed", "CheckButton", CreateButtonStyleBox(ButtonHover));
        theme.SetStylebox("focus", "CheckButton", CreateButtonStyleBox(ButtonNormal));
        theme.SetStylebox("disabled", "CheckButton", CreateButtonStyleBox(ButtonDisabled));

        theme.SetColor("font_color", "CheckButton", TextBody);
        theme.SetFontSize("font_size", "CheckButton", FontSizeBody);
    }

    private static void ApplyOptionButtonStyles(Theme theme)
    {
        theme.SetStylebox("normal", "OptionButton", CreateButtonStyleBox(ButtonNormal));
        theme.SetStylebox("hover", "OptionButton", CreateButtonStyleBox(ButtonHover));
        theme.SetStylebox("pressed", "OptionButton", CreateButtonStyleBox(ButtonPressed));
        theme.SetStylebox("disabled", "OptionButton", CreateButtonStyleBox(ButtonDisabled));
        theme.SetStylebox("focus", "OptionButton", CreateButtonStyleBox(ButtonHover));

        theme.SetColor("font_color", "OptionButton", TextBody);
        theme.SetFontSize("font_size", "OptionButton", FontSizeBody);
    }

    private static void ApplyLineEditStyles(Theme theme)
    {
        StyleBoxFlat normalStyle = new();
        normalStyle.BgColor = new Color(0.12f, 0.10f, 0.08f, 0.95f);
        normalStyle.SetBorderWidthAll(BorderWidthThin);
        normalStyle.BorderColor = PanelBorder;
        normalStyle.SetContentMarginAll(6f);

        StyleBoxFlat focusStyle = new();
        focusStyle.BgColor = new Color(0.15f, 0.13f, 0.10f, 0.95f);
        focusStyle.SetBorderWidthAll(BorderWidthThin);
        focusStyle.BorderColor = AccentGreen;
        focusStyle.SetContentMarginAll(6f);

        theme.SetStylebox("normal", "LineEdit", normalStyle);
        theme.SetStylebox("focus", "LineEdit", focusStyle);

        theme.SetColor("font_color", "LineEdit", TextBody);
        theme.SetColor("caret_color", "LineEdit", AccentGreen);
        theme.SetColor("selection_color", "LineEdit", new Color(0.30f, 0.50f, 0.30f, 0.60f));
        theme.SetFontSize("font_size", "LineEdit", FontSizeBody);
    }

    private static void ApplyHSeparatorStyles(Theme theme)
    {
        StyleBoxFlat separatorStyle = new();
        separatorStyle.BgColor = PanelBorder;
        separatorStyle.ContentMarginTop = 1f;
        separatorStyle.ContentMarginBottom = 1f;

        theme.SetStylebox("separator", "HSeparator", separatorStyle);
    }

    private static StyleBoxFlat CreateButtonStyleBox(Color backgroundColor)
    {
        StyleBoxFlat style = new();
        style.BgColor = backgroundColor;
        style.SetBorderWidthAll(BorderWidth);
        style.BorderColor = ButtonBorder;
        style.SetCornerRadiusAll(0);
        style.SetContentMarginAll(8f);
        return style;
    }
}
