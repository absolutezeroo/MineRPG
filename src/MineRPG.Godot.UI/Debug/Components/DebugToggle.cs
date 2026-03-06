#if DEBUG
using System;

using Godot;

namespace MineRPG.Godot.UI.Debug.Components;

/// <summary>
/// A toggle switch for the debug menu. Displays a label and on/off state.
/// </summary>
public sealed partial class DebugToggle : HBoxContainer
{
    private readonly string _labelText;
    private readonly Func<bool> _getter;
    private readonly Action<bool> _setter;

    private Label _stateLabel = null!;

    /// <summary>
    /// Creates a debug toggle.
    /// </summary>
    /// <param name="labelText">Display label for this toggle.</param>
    /// <param name="getter">Function to read current state.</param>
    /// <param name="setter">Function to apply new state.</param>
    public DebugToggle(string labelText, Func<bool> getter, Action<bool> setter)
    {
        _labelText = labelText;
        _getter = getter;
        _setter = setter;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;

        Label nameLabel = new();
        nameLabel.Text = _labelText;
        nameLabel.CustomMinimumSize = new Vector2(200, 0);
        DebugTheme.ApplyLabelStyle(nameLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(nameLabel);

        _stateLabel = new Label();
        _stateLabel.CustomMinimumSize = new Vector2(40, 0);
        _stateLabel.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_stateLabel);

        UpdateStateLabel();
    }

    /// <inheritdoc />
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton &&
            mouseButton.ButtonIndex == MouseButton.Left &&
            mouseButton.Pressed)
        {
            bool currentValue = _getter();
            _setter(!currentValue);
            UpdateStateLabel();
            AcceptEvent();
        }
    }

    /// <summary>
    /// Refreshes the display to match the current value.
    /// </summary>
    public void Refresh() => UpdateStateLabel();

    private void UpdateStateLabel()
    {
        bool value = _getter();
        _stateLabel.Text = value ? "ON" : "OFF";
        Color stateColor = value ? DebugTheme.ToggleOnColor : DebugTheme.ToggleOffColor;
        DebugTheme.ApplyLabelStyle(_stateLabel, stateColor, DebugTheme.FontSizeSmall);
    }
}
#endif
