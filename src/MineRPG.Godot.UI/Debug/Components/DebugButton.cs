#if DEBUG
using System;

using Godot;

namespace MineRPG.Godot.UI.Debug.Components;

/// <summary>
/// A styled button for the debug menu. Triggers an action on click.
/// </summary>
public sealed partial class DebugButton : Button
{
    private Action _action = null!;

    /// <summary>
    /// Creates and initializes a styled DebugButton.
    /// </summary>
    /// <param name="labelText">Button label text.</param>
    /// <param name="action">Action to invoke on click.</param>
    /// <returns>The configured button instance.</returns>
    public static DebugButton Create(string labelText, Action action)
    {
        DebugButton button = new();
        button._action = action;
        button.Text = labelText;
        return button;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
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
