using Godot;

using MineRPG.Core.DI;
using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;

namespace MineRPG.Godot.UI.HUD;

/// <summary>
/// Displays 4 always-visible survival bars (health, hunger, thirst, stamina)
/// and 2 contextual bars (breath, temperature) that appear only when relevant.
/// Purely event-driven — no <c>_Process()</c> needed.
/// </summary>
public sealed partial class SurvivalBarsNode : Control
{
    private const float BarWidth = 140f;
    private const float BarHeight = 12f;
    private const float BarSpacing = 3f;
    private const float Margin = 10f;
    private const int FontSize = 10;

    private IEventBus _eventBus = null!;
    private ILogger _logger = null!;

    private ProgressBar _healthBar = null!;
    private ProgressBar _hungerBar = null!;
    private ProgressBar _thirstBar = null!;
    private ProgressBar _staminaBar = null!;
    private ProgressBar _breathBar = null!;
    private ProgressBar _temperatureBar = null!;

    private Label _healthLabel = null!;
    private Label _hungerLabel = null!;
    private Label _thirstLabel = null!;
    private Label _staminaLabel = null!;
    private Label _breathLabel = null!;
    private Label _temperatureLabel = null!;

    private HBoxContainer _breathRow = null!;
    private HBoxContainer _temperatureRow = null!;

    /// <inheritdoc />
    public override void _Ready()
    {
        _eventBus = ServiceLocator.Instance.Get<IEventBus>();
        _logger = ServiceLocator.Instance.Get<ILogger>();

        MouseFilter = MouseFilterEnum.Ignore;

        VBoxContainer container = new();
        container.Name = "SurvivalBarsContainer";
        container.AddThemeConstantOverride("separation", (int)BarSpacing);
        AddChild(container);

        _healthBar = CreateBar(container, "Health", GameTheme.BarHealth, out _healthLabel, out HBoxContainer _);
        _hungerBar = CreateBar(container, "Hunger", GameTheme.BarHunger, out _hungerLabel, out HBoxContainer _);
        _thirstBar = CreateBar(container, "Thirst", GameTheme.BarThirst, out _thirstLabel, out HBoxContainer _);
        _staminaBar = CreateBar(container, "Stamina", GameTheme.BarStamina, out _staminaLabel, out HBoxContainer _);
        _breathBar = CreateBar(container, "Breath", GameTheme.BarBreath, out _breathLabel, out _breathRow);
        _temperatureBar = CreateBar(container, "Temp", GameTheme.BarTemperatureHot, out _temperatureLabel, out _temperatureRow);

        // Contextual bars start hidden
        _breathRow.Visible = false;
        _temperatureRow.Visible = false;

        // Anchor bottom-left
        AnchorLeft = 0f;
        AnchorTop = 1f;
        AnchorRight = 0f;
        AnchorBottom = 1f;
        GrowHorizontal = GrowDirection.End;
        GrowVertical = GrowDirection.Begin;
        OffsetLeft = Margin;
        OffsetBottom = -70f;
        OffsetRight = Margin + BarWidth + 60f;
        OffsetTop = -250f;

        SubscribeEvents();

        _logger.Info("SurvivalBarsNode ready.");
    }

    /// <inheritdoc />
    public override void _ExitTree() => UnsubscribeEvents();

    private void SubscribeEvents()
    {
        _eventBus.Subscribe<HealthChangedEvent>(OnHealthChanged);
        _eventBus.Subscribe<HungerChangedEvent>(OnHungerChanged);
        _eventBus.Subscribe<ThirstChangedEvent>(OnThirstChanged);
        _eventBus.Subscribe<StaminaChangedEvent>(OnStaminaChanged);
        _eventBus.Subscribe<BreathChangedEvent>(OnBreathChanged);
        _eventBus.Subscribe<PlayerTemperatureChangedEvent>(OnTemperatureChanged);
        _eventBus.Subscribe<PlayerStartedDrowningEvent>(OnStartedDrowning);
        _eventBus.Subscribe<PlayerStoppedDrowningEvent>(OnStoppedDrowning);
    }

    private void UnsubscribeEvents()
    {
        _eventBus.Unsubscribe<HealthChangedEvent>(OnHealthChanged);
        _eventBus.Unsubscribe<HungerChangedEvent>(OnHungerChanged);
        _eventBus.Unsubscribe<ThirstChangedEvent>(OnThirstChanged);
        _eventBus.Unsubscribe<StaminaChangedEvent>(OnStaminaChanged);
        _eventBus.Unsubscribe<BreathChangedEvent>(OnBreathChanged);
        _eventBus.Unsubscribe<PlayerTemperatureChangedEvent>(OnTemperatureChanged);
        _eventBus.Unsubscribe<PlayerStartedDrowningEvent>(OnStartedDrowning);
        _eventBus.Unsubscribe<PlayerStoppedDrowningEvent>(OnStoppedDrowning);
    }

    private void OnHealthChanged(HealthChangedEvent evt)
    {
        _healthBar.Value = evt.NewValue;
        _healthBar.MaxValue = evt.MaxValue;
        _healthLabel.Text = $"{evt.NewValue:F0}/{evt.MaxValue:F0}";
    }

    private void OnHungerChanged(HungerChangedEvent evt)
    {
        _hungerBar.Value = evt.Hunger;
        _hungerBar.MaxValue = evt.MaxHunger;
        _hungerLabel.Text = $"{evt.Hunger:F0}/{evt.MaxHunger:F0}";
    }

    private void OnThirstChanged(ThirstChangedEvent evt)
    {
        _thirstBar.Value = evt.NewValue;
        _thirstBar.MaxValue = evt.MaxValue;
        _thirstLabel.Text = $"{evt.NewValue:F0}/{evt.MaxValue:F0}";
    }

    private void OnStaminaChanged(StaminaChangedEvent evt)
    {
        _staminaBar.Value = evt.NewValue;
        _staminaBar.MaxValue = evt.MaxValue;
        _staminaLabel.Text = $"{evt.NewValue:F0}/{evt.MaxValue:F0}";
    }

    private void OnBreathChanged(BreathChangedEvent evt)
    {
        _breathBar.Value = evt.NewValue;
        _breathBar.MaxValue = evt.MaxValue;
        _breathLabel.Text = $"{evt.NewValue:F1}/{evt.MaxValue:F0}";

        // Show breath bar when it's not full
        _breathRow.Visible = evt.NewValue < evt.MaxValue;
    }

    private void OnTemperatureChanged(PlayerTemperatureChangedEvent evt)
    {
        bool shouldShow = evt.IsOverheating || evt.IsFreezing;
        _temperatureRow.Visible = shouldShow;

        if (!shouldShow)
        {
            return;
        }

        // Map temperature to bar (0 = danger, max = safe)
        if (evt.IsOverheating)
        {
            StyleFill(_temperatureBar, GameTheme.BarTemperatureHot);
            _temperatureLabel.Text = "HOT";
        }
        else
        {
            StyleFill(_temperatureBar, GameTheme.BarTemperatureCold);
            _temperatureLabel.Text = "COLD";
        }

        // Use absolute distance from comfort zone as "danger level"
        float danger = evt.IsOverheating
            ? evt.NormalizedTemperature
            : -evt.NormalizedTemperature;
        _temperatureBar.Value = danger;
        _temperatureBar.MaxValue = 1.0;
    }

    private void OnStartedDrowning(PlayerStartedDrowningEvent evt) => _breathRow.Visible = true;

    private void OnStoppedDrowning(PlayerStoppedDrowningEvent evt)
    {
        // Breath bar hides when full (handled by OnBreathChanged)
    }

    private static ProgressBar CreateBar(
        VBoxContainer parent,
        string label,
        Color fillColor,
        out Label valueLabel,
        out HBoxContainer row)
    {
        row = new HBoxContainer();
        row.Name = $"{label}Row";
        row.AddThemeConstantOverride("separation", 4);
        parent.AddChild(row);

        Label nameLabel = new();
        nameLabel.Name = $"{label}Name";
        nameLabel.Text = label;
        nameLabel.CustomMinimumSize = new Vector2(50f, 0f);
        nameLabel.HorizontalAlignment = HorizontalAlignment.Right;
        nameLabel.AddThemeFontSizeOverride("font_size", FontSize);
        nameLabel.MouseFilter = MouseFilterEnum.Ignore;
        row.AddChild(nameLabel);

        ProgressBar bar = new();
        bar.Name = $"{label}Bar";
        bar.CustomMinimumSize = new Vector2(BarWidth, BarHeight);
        bar.MaxValue = 100;
        bar.Value = 100;
        bar.ShowPercentage = false;
        bar.MouseFilter = MouseFilterEnum.Ignore;
        bar.SizeFlagsHorizontal = SizeFlags.Fill;
        row.AddChild(bar);

        StyleFill(bar, fillColor);
        StyleBackground(bar);

        valueLabel = new Label();
        valueLabel.Name = $"{label}Value";
        valueLabel.Text = "";
        valueLabel.CustomMinimumSize = new Vector2(45f, 0f);
        valueLabel.HorizontalAlignment = HorizontalAlignment.Left;
        valueLabel.AddThemeFontSizeOverride("font_size", FontSize);
        valueLabel.MouseFilter = MouseFilterEnum.Ignore;
        row.AddChild(valueLabel);

        return bar;
    }

    private static void StyleFill(ProgressBar bar, Color fillColor)
    {
        StyleBoxFlat fillStyle = new();
        fillStyle.BgColor = fillColor;
        fillStyle.SetCornerRadiusAll(2);
        bar.AddThemeStyleboxOverride("fill", fillStyle);
    }

    private static void StyleBackground(ProgressBar bar)
    {
        StyleBoxFlat bgStyle = new();
        bgStyle.BgColor = GameTheme.BarBackground;
        bgStyle.SetCornerRadiusAll(2);
        bar.AddThemeStyleboxOverride("background", bgStyle);
    }
}
