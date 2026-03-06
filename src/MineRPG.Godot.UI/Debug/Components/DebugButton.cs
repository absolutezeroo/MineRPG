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

        AddThemeStyleboxOverride("normal", DebugTheme.CreateButtonStyle(new Color(0.15f, 0.2f, 0.3f, 0.8f)));
        AddThemeStyleboxOverride("hover", DebugTheme.CreateButtonStyle(new Color(0.2f, 0.3f, 0.45f, 0.9f)));
        AddThemeStyleboxOverride("pressed", DebugTheme.CreateButtonStyle(new Color(0.1f, 0.15f, 0.25f, 0.9f)));
        AddThemeColorOverride("font_color", DebugTheme.TextPrimary);
        AddThemeFontSizeOverride("font_size", DebugTheme.FontSizeSmall);

        Pressed += OnPressed;
    }

    private void OnPressed() => _action();
}
#endif
