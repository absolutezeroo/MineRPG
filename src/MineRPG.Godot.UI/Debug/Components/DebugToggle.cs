#if DEBUG
using System;

using Godot;

namespace MineRPG.Godot.UI.Debug.Components;

/// <summary>
/// A toggle switch for the debug menu. Displays a label and on/off state.
/// Layout is defined in Scenes/UI/Debug/Widgets/DebugToggle.tscn.
/// </summary>
public sealed partial class DebugToggle : HBoxContainer
{
    private const string ScenePath = "res://Scenes/UI/Debug/Widgets/DebugToggle.tscn";

    private static PackedScene? _sceneCache;

    [Export] private Label _nameLabel = null!;
    [Export] private Label _stateLabel = null!;

    private Func<bool> _getter = null!;
    private Action<bool> _setter = null!;

    /// <summary>
    /// Creates and initializes a DebugToggle from the scene template.
    /// </summary>
    /// <param name="labelText">Display label for this toggle.</param>
    /// <param name="getter">Function to read current state.</param>
    /// <param name="setter">Function to apply new state.</param>
    /// <returns>The configured toggle instance.</returns>
    public static DebugToggle Create(string labelText, Func<bool> getter, Action<bool> setter)
    {
        _sceneCache ??= GD.Load<PackedScene>(ScenePath);
        DebugToggle instance = _sceneCache.Instantiate<DebugToggle>();
        instance._getter = getter;
        instance._setter = setter;
        instance._nameLabel.Text = labelText;
        return instance;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
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
