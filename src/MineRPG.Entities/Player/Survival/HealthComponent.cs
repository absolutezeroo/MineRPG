using System;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces;

namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Tracks player health with Minecraft-style natural regeneration.
/// Regen requires hunger above a configurable threshold.
/// Starvation and environmental damage are applied externally via <see cref="TakeDamage"/>.
/// </summary>
public sealed class HealthComponent : ITickable
{
    private const float PublishThreshold = 0.5f;

    private readonly HealthSettings _settings;
    private readonly IEventBus _eventBus;

    private float _current;
    private float _regenDelayTimer;
    private float _lastPublishedValue;

    /// <summary>
    /// Creates a new health component with default full health.
    /// </summary>
    /// <param name="settings">Health tuning parameters.</param>
    /// <param name="eventBus">Event bus for publishing health changes.</param>
    public HealthComponent(HealthSettings settings, IEventBus eventBus)
    {
        _settings = settings;
        _eventBus = eventBus;
        _current = settings.MaxHealth;
        _lastPublishedValue = _current;
    }

    /// <summary>Current health points.</summary>
    public float Current => _current;

    /// <summary>Maximum health points.</summary>
    public float Max => _settings.MaxHealth;

    /// <summary>Whether the player is dead (health reached zero).</summary>
    public bool IsDead => _current <= 0f;

    /// <summary>
    /// Applies damage to the player and resets the regen delay timer.
    /// </summary>
    /// <param name="amount">Damage points to apply. Must be positive.</param>
    public void TakeDamage(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _current = MathF.Max(0f, _current - amount);
        _regenDelayTimer = _settings.NaturalRegenDelay;
        PublishIfChanged();
    }

    /// <summary>
    /// Restores health without resetting the regen delay.
    /// </summary>
    /// <param name="amount">Health points to restore. Must be positive.</param>
    public void Heal(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _current = MathF.Min(_settings.MaxHealth, _current + amount);
        PublishIfChanged();
    }

    /// <summary>
    /// Advances natural regeneration. Called each tick by the survival system.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    /// <param name="currentHunger">Current hunger level for regen gating.</param>
    public void Tick(float deltaTime, float currentHunger)
    {
        if (IsDead)
        {
            return;
        }

        if (_regenDelayTimer > 0f)
        {
            _regenDelayTimer -= deltaTime;
            return;
        }

        if (currentHunger >= _settings.HungerRegenThreshold &&
            _current < _settings.MaxHealth)
        {
            _current = MathF.Min(
                _settings.MaxHealth,
                _current + _settings.NaturalRegenRate * deltaTime);
            PublishIfChanged();
        }
    }

    /// <inheritdoc />
    void ITickable.Tick(float deltaTime) => Tick(deltaTime, 0f);

    /// <summary>
    /// Sets the current health directly. Used for save/load.
    /// </summary>
    /// <param name="value">Health value to restore.</param>
    public void SetCurrent(float value)
    {
        _current = Math.Clamp(value, 0f, _settings.MaxHealth);
        _lastPublishedValue = _current;
    }

    /// <summary>
    /// Resets health to maximum and clears all timers.
    /// </summary>
    public void Reset()
    {
        _current = _settings.MaxHealth;
        _regenDelayTimer = 0f;
        _lastPublishedValue = _current;
        PublishIfChanged();
    }

    private void PublishIfChanged()
    {
        float delta = MathF.Abs(_current - _lastPublishedValue);

        if (delta >= PublishThreshold || _current <= 0f || _current >= _settings.MaxHealth)
        {
            _lastPublishedValue = _current;
            _eventBus.Publish(new HealthChangedEvent
            {
                NewValue = _current,
                MaxValue = _settings.MaxHealth,
            });
        }
    }
}
