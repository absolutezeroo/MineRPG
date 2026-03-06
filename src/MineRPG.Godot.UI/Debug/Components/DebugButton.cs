#if DEBUG
using System;

using Godot;

namespace MineRPG.Godot.UI.Debug.Components;

/// <summary>
/// A styled button for the debug menu. Triggers an action on click.
/// </summary>
public sealed partial class DebugButton : Button
{
    private readonly string _labelText;
    private readonly Action _action;

    /// <summary>
    /// Creates a debug button.
    /// </summary>
    /// <param name="labelText">Button label text.</param>
    /// <param name="action">Action to invoke on click.</param>
    public DebugButton(string labelText, Action action)
    {
        _labelText = labelText;
        _action = action;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        Text = _labelText;

        StyleBoxFlat normalStyle = new();
        normalStyle.BgColor = new Color(0.15f, 0.2f, 0.3f, 0.8f);
        normalStyle.ContentMarginLeft = 8f;
        normalStyle.ContentMarginRight = 8f;
        normalStyle.ContentMarginTop = 4f;
        normalStyle.ContentMarginBottom = 4f;
        normalStyle.CornerRadiusTopLeft = 3;
        normalStyle.CornerRadiusTopRight = 3;
        normalStyle.CornerRadiusBottomLeft = 3;
        normalStyle.CornerRadiusBottomRight = 3;

        StyleBoxFlat hoverStyle = new();
        hoverStyle.BgColor = new Color(0.2f, 0.3f, 0.45f, 0.9f);
        hoverStyle.ContentMarginLeft = 8f;
        hoverStyle.ContentMarginRight = 8f;
        hoverStyle.ContentMarginTop = 4f;
        hoverStyle.ContentMarginBottom = 4f;
        hoverStyle.CornerRadiusTopLeft = 3;
        hoverStyle.CornerRadiusTopRight = 3;
        hoverStyle.CornerRadiusBottomLeft = 3;
        hoverStyle.CornerRadiusBottomRight = 3;

        StyleBoxFlat pressedStyle = new();
        pressedStyle.BgColor = new Color(0.1f, 0.15f, 0.25f, 0.9f);
        pressedStyle.ContentMarginLeft = 8f;
        pressedStyle.ContentMarginRight = 8f;
        pressedStyle.ContentMarginTop = 4f;
        pressedStyle.ContentMarginBottom = 4f;
        pressedStyle.CornerRadiusTopLeft = 3;
        pressedStyle.CornerRadiusTopRight = 3;
        pressedStyle.CornerRadiusBottomLeft = 3;
        pressedStyle.CornerRadiusBottomRight = 3;

        AddThemeStyleboxOverride("normal", normalStyle);
        AddThemeStyleboxOverride("hover", hoverStyle);
        AddThemeStyleboxOverride("pressed", pressedStyle);
        AddThemeColorOverride("font_color", DebugTheme.TextPrimary);
        AddThemeFontSizeOverride("font_size", DebugTheme.FontSizeSmall);

        Pressed += OnPressed;
    }

    private void OnPressed()
    {
        _action();
    }
}
#endif
