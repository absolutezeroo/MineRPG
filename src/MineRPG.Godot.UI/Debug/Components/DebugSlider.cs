#if DEBUG
using System;

using Godot;

namespace MineRPG.Godot.UI.Debug.Components;

/// <summary>
/// A slider with label and value display for the debug menu.
/// Layout is defined in Scenes/UI/Debug/Widgets/DebugSlider.tscn.
/// </summary>
public sealed partial class DebugSlider : HBoxContainer
{
    private const string ScenePath = "res://Scenes/UI/Debug/Widgets/DebugSlider.tscn";

    private static PackedScene? _sceneCache;

    [Export] private Label _nameLabel = null!;
    [Export] private HSlider _slider = null!;
    [Export] private Label _valueLabel = null!;

    private float _step = 1f;
    private Func<float> _getter = null!;
    private Action<float> _setter = null!;

    /// <summary>
    /// Creates and initializes a DebugSlider from the scene template.
    /// </summary>
    /// <param name="labelText">Display label.</param>
    /// <param name="minValue">Minimum slider value.</param>
    /// <param name="maxValue">Maximum slider value.</param>
    /// <param name="step">Step increment.</param>
    /// <param name="getter">Function to read current value.</param>
    /// <param name="setter">Function to apply new value.</param>
    /// <returns>The configured slider instance.</returns>
    public static DebugSlider Create(
        string labelText,
        float minValue,
        float maxValue,
        float step,
        Func<float> getter,
        Action<float> setter)
    {
        _sceneCache ??= GD.Load<PackedScene>(ScenePath);
        DebugSlider instance = _sceneCache.Instantiate<DebugSlider>();
        instance._step = step;
        instance._getter = getter;
        instance._setter = setter;
        instance._nameLabel.Text = labelText;
        instance._slider.MinValue = minValue;
        instance._slider.MaxValue = maxValue;
        instance._slider.Step = step;
        instance._slider.Value = getter();
        return instance;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        UpdateValueLabel();
        _slider.ValueChanged += OnValueChanged;
    }

    /// <summary>
    /// Refreshes the display to match the current value.
    /// </summary>
    public void Refresh()
    {
        _slider.Value = _getter();
        UpdateValueLabel();
    }

    private void OnValueChanged(double value)
    {
        _setter((float)value);
        UpdateValueLabel();
    }

    private void UpdateValueLabel()
    {
        float value = _getter();

        if (_step >= 1f)
        {
            _valueLabel.Text = ((int)value).ToString();
        }
        else
        {
            _valueLabel.Text = value.ToString("F1");
        }
    }
}
#endif
