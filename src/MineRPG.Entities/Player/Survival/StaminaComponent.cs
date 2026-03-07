using System;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces;

namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Tracks player stamina for sprinting. Drains while sprinting,
/// regenerates with a delay after sprinting stops.
/// </summary>
public sealed class StaminaComponent : ITickable
{
    private const float PublishThreshold = 2f;

    private readonly StaminaSettings _settings;
    private readonly IEventBus _eventBus;

    private float _current;
    private float _regenDelayTimer;
    private float _lastPublishedValue;

    /// <summary>
    /// Creates a new stamina component at full stamina.
    /// </summary>
    /// <param name="settings">Stamina tuning parameters.</param>
    /// <param name="eventBus">Event bus for publishing stamina changes.</param>
    public StaminaComponent(StaminaSettings settings, IEventBus eventBus)
    {
        _settings = settings;
        _eventBus = eventBus;
        _current = settings.MaxStamina;
        _lastPublishedValue = _current;
    }

    /// <summary>Current stamina points.</summary>
    public float Current => _current;

    /// <summary>Maximum stamina points.</summary>
    public float Max => _settings.MaxStamina;

    /// <summary>Whether the player has enough stamina to start sprinting.</summary>
    public bool CanSprint => _current >= _settings.MinStaminaToSprint;

    /// <summary>
    /// Drains stamina for sprinting. Returns false when stamina is exhausted.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    /// <returns>True if stamina remains, false if exhausted.</returns>
    public bool DrainForSprint(float deltaTime)
    {
        _current = MathF.Max(0f, _current - _settings.SprintDrainRate * deltaTime);
        _regenDelayTimer = _settings.RegenDelay;
        PublishIfChanged();

        return _current > 0f;
    }

    /// <summary>
    /// Regenerates stamina when not sprinting. Waits for the regen delay first.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    public void Tick(float deltaTime)
    {
        if (_current >= _settings.MaxStamina)
        {
            return;
        }

        if (_regenDelayTimer > 0f)
        {
            _regenDelayTimer -= deltaTime;
            return;
        }

        _current = MathF.Min(_settings.MaxStamina, _current + _settings.RegenRate * deltaTime);
        PublishIfChanged();
    }

    /// <summary>
    /// Sets the current stamina directly. Used for save/load.
    /// </summary>
    /// <param name="value">Stamina value to restore.</param>
    public void SetCurrent(float value)
    {
        _current = Math.Clamp(value, 0f, _settings.MaxStamina);
        _lastPublishedValue = _current;
    }

    /// <summary>
    /// Resets stamina to maximum and clears the regen delay.
    /// </summary>
    public void Reset()
    {
        _current = _settings.MaxStamina;
        _regenDelayTimer = 0f;
        _lastPublishedValue = _current;
        PublishIfChanged();
    }

    private void PublishIfChanged()
    {
        float delta = MathF.Abs(_current - _lastPublishedValue);

        if (delta >= PublishThreshold || _current <= 0f || _current >= _settings.MaxStamina)
        {
            _lastPublishedValue = _current;
            _eventBus.Publish(new StaminaChangedEvent
            {
                NewValue = _current,
                MaxValue = _settings.MaxStamina,
            });
        }
    }
}
