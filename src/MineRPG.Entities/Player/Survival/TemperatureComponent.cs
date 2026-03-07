using System;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces;

namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Tracks player body temperature using biome-based environmental input.
/// Body temperature slowly adapts toward the environment.
/// Damage is applied when outside the comfort zone.
/// </summary>
public sealed class TemperatureComponent : ITickable
{
    private const float PublishThreshold = 0.02f;

    private readonly TemperatureSettings _settings;
    private readonly IEventBus _eventBus;

    private float _bodyTemperature;
    private float _environmentTemperature;
    private float _hotDamageTimer;
    private float _coldDamageTimer;
    private float _lastPublishedValue;
    private bool _wasOverheating;
    private bool _wasFreezing;

    /// <summary>
    /// Creates a new temperature component at neutral body temperature.
    /// </summary>
    /// <param name="settings">Temperature tuning parameters.</param>
    /// <param name="eventBus">Event bus for publishing temperature changes.</param>
    public TemperatureComponent(TemperatureSettings settings, IEventBus eventBus)
    {
        _settings = settings;
        _eventBus = eventBus;
        _bodyTemperature = (settings.ComfortMin + settings.ComfortMax) / 2f;
        _environmentTemperature = _bodyTemperature;
        _lastPublishedValue = _bodyTemperature;
    }

    /// <summary>Current normalized body temperature in [-1, 1].</summary>
    public float Current => _bodyTemperature;

    /// <summary>Current environment temperature after altitude adjustment.</summary>
    public float EnvironmentTemperature => _environmentTemperature;

    /// <summary>Whether the player is overheating (above comfort zone).</summary>
    public bool IsOverheating => _bodyTemperature > _settings.ComfortMax;

    /// <summary>Whether the player is freezing (below comfort zone).</summary>
    public bool IsFreezing => _bodyTemperature < _settings.ComfortMin;

    /// <summary>Whether the player is within the comfort zone.</summary>
    public bool IsComfortable => !IsOverheating && !IsFreezing;

    /// <summary>
    /// Updates the environment temperature from the biome and altitude.
    /// </summary>
    /// <param name="biomeTemperature">Normalized biome temperature in [-1, 1].</param>
    /// <param name="worldY">Player's current world Y position.</param>
    public void NotifyEnvironment(float biomeTemperature, float worldY)
    {
        float altitudeOffset = (worldY - _settings.AltitudeBaseY) * -_settings.AltitudeCoolRate;
        _environmentTemperature = MathF.Max(-1f, MathF.Min(1f, biomeTemperature + altitudeOffset));
    }

    /// <summary>
    /// Adapts body temperature toward the environment and checks for damage.
    /// Returns hot or cold damage to apply, or zero.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    /// <returns>Positive for heat/cold damage, zero if comfortable.</returns>
    public float Tick(float deltaTime)
    {
        float difference = _environmentTemperature - _bodyTemperature;
        float adaptation = _settings.AdaptationRate * deltaTime;
        _bodyTemperature += MathF.Min(MathF.Abs(difference), adaptation) * MathF.Sign(difference);
        _bodyTemperature = MathF.Max(-1f, MathF.Min(1f, _bodyTemperature));

        PublishIfChanged();

        float damage = 0f;

        if (IsOverheating)
        {
            _coldDamageTimer = 0f;
            _hotDamageTimer += deltaTime;

            if (_hotDamageTimer >= _settings.HotDamageInterval)
            {
                _hotDamageTimer -= _settings.HotDamageInterval;
                damage = _settings.HotDamageAmount;
            }
        }
        else if (IsFreezing)
        {
            _hotDamageTimer = 0f;
            _coldDamageTimer += deltaTime;

            if (_coldDamageTimer >= _settings.ColdDamageInterval)
            {
                _coldDamageTimer -= _settings.ColdDamageInterval;
                damage = _settings.ColdDamageAmount;
            }
        }
        else
        {
            _hotDamageTimer = 0f;
            _coldDamageTimer = 0f;
        }

        return damage;
    }

    /// <inheritdoc />
    void ITickable.Tick(float deltaTime) => Tick(deltaTime);

    /// <summary>
    /// Sets the body temperature directly. Used for save/load.
    /// </summary>
    /// <param name="value">Normalized body temperature in [-1, 1].</param>
    public void SetCurrent(float value)
    {
        _bodyTemperature = Math.Clamp(value, -1f, 1f);
        _lastPublishedValue = _bodyTemperature;
    }

    /// <summary>
    /// Resets body temperature to the comfort zone center and clears damage timers.
    /// </summary>
    public void Reset()
    {
        _bodyTemperature = (_settings.ComfortMin + _settings.ComfortMax) / 2f;
        _environmentTemperature = _bodyTemperature;
        _hotDamageTimer = 0f;
        _coldDamageTimer = 0f;

        // Publish before zeroing state flags so the event detects the transition
        PublishIfChanged();

        _wasOverheating = false;
        _wasFreezing = false;
        _lastPublishedValue = _bodyTemperature;
    }

    private void PublishIfChanged()
    {
        float delta = MathF.Abs(_bodyTemperature - _lastPublishedValue);
        bool isOverheatingNow = IsOverheating;
        bool isFreezingNow = IsFreezing;
        bool stateChanged = isOverheatingNow != _wasOverheating || isFreezingNow != _wasFreezing;

        if (delta >= PublishThreshold || stateChanged)
        {
            _lastPublishedValue = _bodyTemperature;
            _wasOverheating = isOverheatingNow;
            _wasFreezing = isFreezingNow;
            _eventBus.Publish(new PlayerTemperatureChangedEvent
            {
                NormalizedTemperature = _bodyTemperature,
                IsOverheating = isOverheatingNow,
                IsFreezing = isFreezingNow,
            });
        }
    }
}
