using System;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces;

namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Tracks player hunger and saturation using Minecraft-style mechanics.
/// Saturation is consumed before hunger. Both decay passively over time.
/// </summary>
public sealed class HungerComponent : ITickable
{
    private const float PublishThreshold = 0.5f;

    private readonly HungerSettings _settings;
    private readonly IEventBus _eventBus;

    private float _hunger;
    private float _saturation;
    private float _lastPublishedHunger;
    private float _lastPublishedSaturation;

    /// <summary>
    /// Creates a new hunger component at full hunger with default saturation.
    /// </summary>
    /// <param name="settings">Hunger tuning parameters.</param>
    /// <param name="eventBus">Event bus for publishing hunger changes.</param>
    public HungerComponent(HungerSettings settings, IEventBus eventBus)
    {
        _settings = settings;
        _eventBus = eventBus;
        _hunger = settings.MaxHunger;
        _saturation = settings.InitialSaturation;
        _lastPublishedHunger = _hunger;
        _lastPublishedSaturation = _saturation;
    }

    /// <summary>Current hunger points.</summary>
    public float Current => _hunger;

    /// <summary>Current saturation points.</summary>
    public float Saturation => _saturation;

    /// <summary>Maximum hunger points.</summary>
    public float Max => _settings.MaxHunger;

    /// <summary>Whether hunger has reached zero (player is starving).</summary>
    public bool IsStarving => _hunger <= 0f;

    /// <summary>
    /// Applies passive hunger decay. Saturation is consumed first.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    public void Tick(float deltaTime)
    {
        float decay = _settings.PassiveDecayRate * deltaTime;
        ApplyDecay(decay);
    }

    /// <summary>
    /// Applies additional hunger drain from sprinting.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    public void ApplySprintDecay(float deltaTime)
    {
        float decay = _settings.SprintDecayRate * deltaTime;
        ApplyDecay(decay);
    }

    /// <summary>
    /// Restores hunger and saturation from consuming food.
    /// Saturation is capped at the current hunger level after restoration.
    /// </summary>
    /// <param name="hungerRestore">Hunger points to restore.</param>
    /// <param name="saturationRestore">Saturation points to restore.</param>
    public void RestoreFood(float hungerRestore, float saturationRestore)
    {
        _hunger = MathF.Min(_settings.MaxHunger, _hunger + hungerRestore);
        _saturation = MathF.Min(_hunger, _saturation + saturationRestore);
        PublishIfChanged();
    }

    /// <summary>
    /// Sets hunger and saturation directly. Used for save/load.
    /// </summary>
    /// <param name="hunger">Hunger value to restore.</param>
    /// <param name="saturation">Saturation value to restore.</param>
    public void SetValues(float hunger, float saturation)
    {
        _hunger = Math.Clamp(hunger, 0f, _settings.MaxHunger);
        _saturation = Math.Clamp(saturation, 0f, _hunger);
        _lastPublishedHunger = _hunger;
        _lastPublishedSaturation = _saturation;
    }

    /// <summary>
    /// Resets hunger and saturation to starting values.
    /// </summary>
    public void Reset()
    {
        _hunger = _settings.MaxHunger;
        _saturation = _settings.InitialSaturation;
        _lastPublishedHunger = _hunger;
        _lastPublishedSaturation = _saturation;
        PublishIfChanged();
    }

    private void ApplyDecay(float amount)
    {
        if (_saturation > 0f)
        {
            float saturationDrain = MathF.Min(_saturation, amount);
            _saturation -= saturationDrain;
            amount -= saturationDrain;
        }

        if (amount > 0f)
        {
            _hunger = MathF.Max(0f, _hunger - amount);
        }

        PublishIfChanged();
    }

    private void PublishIfChanged()
    {
        float hungerDelta = MathF.Abs(_hunger - _lastPublishedHunger);
        float saturationDelta = MathF.Abs(_saturation - _lastPublishedSaturation);

        if (hungerDelta >= PublishThreshold || saturationDelta >= PublishThreshold
            || _hunger <= 0f || _hunger >= _settings.MaxHunger)
        {
            _lastPublishedHunger = _hunger;
            _lastPublishedSaturation = _saturation;
            _eventBus.Publish(new HungerChangedEvent
            {
                Hunger = _hunger,
                Saturation = _saturation,
                MaxHunger = _settings.MaxHunger,
            });
        }
    }
}
