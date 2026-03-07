using System;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces;

namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Tracks player thirst. Simpler than hunger — no saturation layer.
/// When thirst reaches zero, the player takes periodic dehydration damage.
/// </summary>
public sealed class ThirstComponent : ITickable
{
    private const float PublishThreshold = 0.5f;

    private readonly ThirstSettings _settings;
    private readonly IEventBus _eventBus;

    private float _current;
    private float _dehydrationTimer;
    private float _lastPublishedValue;

    /// <summary>
    /// Creates a new thirst component at full hydration.
    /// </summary>
    /// <param name="settings">Thirst tuning parameters.</param>
    /// <param name="eventBus">Event bus for publishing thirst changes.</param>
    public ThirstComponent(ThirstSettings settings, IEventBus eventBus)
    {
        _settings = settings;
        _eventBus = eventBus;
        _current = settings.MaxThirst;
        _lastPublishedValue = _current;
    }

    /// <summary>Current thirst points.</summary>
    public float Current => _current;

    /// <summary>Maximum thirst points.</summary>
    public float Max => _settings.MaxThirst;

    /// <summary>Whether the player is dehydrated (thirst reached zero).</summary>
    public bool IsDehydrated => _current <= 0f;

    /// <summary>
    /// Applies passive thirst decay.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    public void Tick(float deltaTime)
    {
        _current = MathF.Max(0f, _current - _settings.PassiveDecayRate * deltaTime);
        PublishIfChanged();
    }

    /// <summary>
    /// Applies additional thirst drain from sprinting.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    public void ApplySprintDecay(float deltaTime)
    {
        _current = MathF.Max(0f, _current - _settings.SprintDecayRate * deltaTime);
        PublishIfChanged();
    }

    /// <summary>
    /// Advances the dehydration damage timer when dehydrated.
    /// Returns the damage to apply, or zero if no damage this tick.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    /// <returns>Damage to apply from dehydration, or zero.</returns>
    public float TickDehydrationDamage(float deltaTime)
    {
        if (!IsDehydrated)
        {
            _dehydrationTimer = 0f;
            return 0f;
        }

        _dehydrationTimer += deltaTime;

        if (_dehydrationTimer >= _settings.DehydrationDamageInterval)
        {
            _dehydrationTimer -= _settings.DehydrationDamageInterval;
            return _settings.DehydrationDamageAmount;
        }

        return 0f;
    }

    /// <summary>
    /// Restores thirst from consuming a drink.
    /// </summary>
    /// <param name="amount">Thirst points to restore.</param>
    public void Restore(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _current = MathF.Min(_settings.MaxThirst, _current + amount);
        PublishIfChanged();
    }

    /// <summary>
    /// Sets the current thirst directly. Used for save/load.
    /// </summary>
    /// <param name="value">Thirst value to restore.</param>
    public void SetCurrent(float value)
    {
        _current = Math.Clamp(value, 0f, _settings.MaxThirst);
        _lastPublishedValue = _current;
    }

    /// <summary>
    /// Resets thirst to maximum and clears damage timer.
    /// </summary>
    public void Reset()
    {
        _current = _settings.MaxThirst;
        _dehydrationTimer = 0f;
        _lastPublishedValue = _current;
        PublishIfChanged();
    }

    private void PublishIfChanged()
    {
        float delta = MathF.Abs(_current - _lastPublishedValue);

        if (delta >= PublishThreshold || _current <= 0f || _current >= _settings.MaxThirst)
        {
            _lastPublishedValue = _current;
            _eventBus.Publish(new ThirstChangedEvent
            {
                NewValue = _current,
                MaxValue = _settings.MaxThirst,
            });
        }
    }
}
