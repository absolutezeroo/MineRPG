#if DEBUG
using System;

using Godot;

namespace MineRPG.Godot.UI.Debug.Components;

/// <summary>
/// A slider with label and value display for the debug menu.
/// </summary>
public sealed partial class DebugSlider : HBoxContainer
{
    private const float LabelWidth = 160f;
    private const float ValueWidth = 60f;

    private readonly string _labelText;
    private readonly float _minValue;
    private readonly float _maxValue;
    private readonly float _step;
    private readonly Func<float> _getter;
    private readonly Action<float> _setter;

    private HSlider _slider = null!;
    private Label _valueLabel = null!;

    /// <summary>
    /// Creates a debug slider.
    /// </summary>
    /// <param name="labelText">Display label.</param>
    /// <param name="minValue">Minimum slider value.</param>
    /// <param name="maxValue">Maximum slider value.</param>
    /// <param name="step">Step increment.</param>
    /// <param name="getter">Function to read current value.</param>
    /// <param name="setter">Function to apply new value.</param>
    public DebugSlider(
        string labelText,
        float minValue,
        float maxValue,
        float step,
        Func<float> getter,
        Action<float> setter)
    {
        _labelText = labelText;
        _minValue = minValue;
        _maxValue = maxValue;
        _step = step;
        _getter = getter;
        _setter = setter;
    }

    /// <inheritdoc />
    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;

        Label nameLabel = new();
        nameLabel.Text = _labelText;
        nameLabel.CustomMinimumSize = new Vector2(LabelWidth, 0);
        DebugTheme.ApplyLabelStyle(nameLabel, DebugTheme.TextPrimary, DebugTheme.FontSizeSmall);
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(nameLabel);

        _slider = new HSlider();
        _slider.MinValue = _minValue;
        _slider.MaxValue = _maxValue;
        _slider.Step = _step;
        _slider.Value = _getter();
        _slider.CustomMinimumSize = new Vector2(140, 20);
        _slider.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        AddChild(_slider);

        _valueLabel = new Label();
        _valueLabel.CustomMinimumSize = new Vector2(ValueWidth, 0);
        _valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        DebugTheme.ApplyLabelStyle(_valueLabel, DebugTheme.TextAccent, DebugTheme.FontSizeSmall);
        _valueLabel.MouseFilter = MouseFilterEnum.Ignore;
        AddChild(_valueLabel);

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
