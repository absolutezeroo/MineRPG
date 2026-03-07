using System;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces;

namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Tracks player breath for underwater survival.
/// Drains while submerged, fast regens above water.
/// When breath reaches zero, periodic drowning damage is applied.
/// </summary>
public sealed class BreathComponent : ITickable
{
    private const float PublishThreshold = 0.5f;

    private readonly BreathSettings _settings;
    private readonly IEventBus _eventBus;

    private float _current;
    private float _drowningTimer;
    private bool _wasDrowning;
    private float _lastPublishedValue;

    /// <summary>
    /// Creates a new breath component at full breath.
    /// </summary>
    /// <param name="settings">Breath tuning parameters.</param>
    /// <param name="eventBus">Event bus for publishing breath changes.</param>
    public BreathComponent(BreathSettings settings, IEventBus eventBus)
    {
        _settings = settings;
        _eventBus = eventBus;
        _current = settings.MaxBreath;
        _lastPublishedValue = _current;
    }

    /// <summary>Current breath points (seconds of air remaining).</summary>
    public float Current => _current;

    /// <summary>Maximum breath points.</summary>
    public float Max => _settings.MaxBreath;

    /// <summary>Whether the player has no air and is actively drowning.</summary>
    public bool IsDrowning => _current <= 0f;

    /// <summary>
    /// Updates breath based on whether the player is submerged.
    /// Returns drowning damage to apply, or zero.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    /// <param name="isUnderwater">Whether the player's head is submerged.</param>
    /// <returns>Drowning damage to apply this tick, or zero.</returns>
    public float Tick(float deltaTime, bool isUnderwater)
    {
        if (isUnderwater)
        {
            _current = MathF.Max(0f, _current - _settings.DrainRate * deltaTime);
        }
        else
        {
            _current = MathF.Min(_settings.MaxBreath, _current + _settings.RegenRate * deltaTime);
            _drowningTimer = 0f;
        }

        PublishIfChanged();
        UpdateDrowningState();

        if (!IsDrowning)
        {
            return 0f;
        }

        _drowningTimer += deltaTime;

        if (_drowningTimer >= _settings.DrowningDamageInterval)
        {
            _drowningTimer -= _settings.DrowningDamageInterval;
            return _settings.DrowningDamageAmount;
        }

        return 0f;
    }

    /// <inheritdoc />
    void ITickable.Tick(float deltaTime) => Tick(deltaTime, false);

    /// <summary>
    /// Sets the current breath directly. Used for save/load.
    /// </summary>
    /// <param name="value">Breath value to restore.</param>
    public void SetCurrent(float value)
    {
        _current = Math.Clamp(value, 0f, _settings.MaxBreath);
        _lastPublishedValue = _current;
    }

    /// <summary>
    /// Resets breath to maximum and clears drowning state.
    /// </summary>
    public void Reset()
    {
        _current = _settings.MaxBreath;
        _drowningTimer = 0f;
        _wasDrowning = false;
        _lastPublishedValue = _current;
        PublishIfChanged();
    }

    private void UpdateDrowningState()
    {
        bool isDrowningNow = IsDrowning;

        if (isDrowningNow && !_wasDrowning)
        {
            _eventBus.Publish(new PlayerStartedDrowningEvent());
        }
        else if (!isDrowningNow && _wasDrowning)
        {
            _eventBus.Publish(new PlayerStoppedDrowningEvent());
        }

        _wasDrowning = isDrowningNow;
    }

    private void PublishIfChanged()
    {
        float delta = MathF.Abs(_current - _lastPublishedValue);

        if (delta >= PublishThreshold || _current <= 0f || _current >= _settings.MaxBreath)
        {
            _lastPublishedValue = _current;
            _eventBus.Publish(new BreathChangedEvent
            {
                NewValue = _current,
                MaxValue = _settings.MaxBreath,
            });
        }
    }
}
