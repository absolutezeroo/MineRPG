using FluentAssertions;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player.Survival;

using Xunit;

namespace MineRPG.Tests.Entities.Survival;

public sealed class TemperatureComponentTests
{
    private readonly EventBus _eventBus = new(NullLogger.Instance);
    private readonly TemperatureSettings _settings = new()
    {
        ComfortMin = -0.15f,
        ComfortMax = 0.35f,
        AltitudeCoolRate = 0.0015f,
        AltitudeBaseY = 64,
        HotDamageInterval = 3f,
        HotDamageAmount = 1f,
        ColdDamageInterval = 3f,
        ColdDamageAmount = 1f,
        AdaptationRate = 0.5f,
    };

    [Fact]
    public void Constructor_StartsAtComfortZoneCenter()
    {
        TemperatureComponent component = new(_settings, _eventBus);

        float expectedCenter = (_settings.ComfortMin + _settings.ComfortMax) / 2f;
        component.Current.Should().BeApproximately(expectedCenter, 0.001f);
        component.IsComfortable.Should().BeTrue();
        component.IsOverheating.Should().BeFalse();
        component.IsFreezing.Should().BeFalse();
    }

    [Fact]
    public void NotifyEnvironment_AppliesAltitudeCorrection()
    {
        TemperatureComponent component = new(_settings, _eventBus);

        // 100 blocks above baseline: -0.0015 * 100 = -0.15 offset
        component.NotifyEnvironment(0.5f, 164f);

        component.EnvironmentTemperature.Should().BeApproximately(0.35f, 0.01f);
    }

    [Fact]
    public void Tick_AdaptsTowardEnvironment()
    {
        TemperatureComponent component = new(_settings, _eventBus);
        component.NotifyEnvironment(0.8f, 64f);

        // Adaptation rate = 0.5/s for 1s
        component.Tick(1f);

        // Should move toward 0.8 from center (0.1)
        component.Current.Should().BeGreaterThan(0.1f);
    }

    [Fact]
    public void Tick_Overheating_ReturnsDamageAfterInterval()
    {
        TemperatureSettings fastSettings = new()
        {
            ComfortMin = -0.15f,
            ComfortMax = 0.35f,
            AdaptationRate = 10f,
            HotDamageInterval = 1f,
            HotDamageAmount = 2f,
            ColdDamageInterval = 3f,
            ColdDamageAmount = 1f,
        };

        TemperatureComponent component = new(fastSettings, _eventBus);
        component.NotifyEnvironment(0.9f, 64f);

        // Adapt quickly to hot environment
        component.Tick(2f);

        component.IsOverheating.Should().BeTrue();

        // Now should take damage after interval
        float damage = component.Tick(1f);

        damage.Should().Be(2f);
    }

    [Fact]
    public void Tick_Freezing_ReturnsDamageAfterInterval()
    {
        TemperatureSettings fastSettings = new()
        {
            ComfortMin = -0.15f,
            ComfortMax = 0.35f,
            AdaptationRate = 10f,
            HotDamageInterval = 3f,
            HotDamageAmount = 1f,
            ColdDamageInterval = 1f,
            ColdDamageAmount = 3f,
        };

        TemperatureComponent component = new(fastSettings, _eventBus);
        component.NotifyEnvironment(-0.8f, 64f);

        // Adapt quickly to cold environment
        component.Tick(2f);

        component.IsFreezing.Should().BeTrue();

        float damage = component.Tick(1f);

        damage.Should().Be(3f);
    }

    [Fact]
    public void Tick_Comfortable_ReturnsZeroDamage()
    {
        TemperatureComponent component = new(_settings, _eventBus);
        component.NotifyEnvironment(0.1f, 64f);

        float damage = component.Tick(10f);

        damage.Should().Be(0f);
        component.IsComfortable.Should().BeTrue();
    }

    [Fact]
    public void Reset_RestoresToCenter()
    {
        TemperatureComponent component = new(_settings, _eventBus);
        component.NotifyEnvironment(0.9f, 64f);
        component.Tick(10f);

        component.Reset();

        float expectedCenter = (_settings.ComfortMin + _settings.ComfortMax) / 2f;
        component.Current.Should().BeApproximately(expectedCenter, 0.001f);
        component.IsComfortable.Should().BeTrue();
    }

    [Fact]
    public void Tick_PublishesTemperatureChangedEvent()
    {
        TemperatureComponent component = new(_settings, _eventBus);
        PlayerTemperatureChangedEvent? received = null;
        _eventBus.Subscribe<PlayerTemperatureChangedEvent>(e => received = e);

        component.NotifyEnvironment(0.8f, 64f);
        component.Tick(5f);

        received.Should().NotBeNull();
    }

    [Fact]
    public void SetCurrent_ClampsToRange()
    {
        TemperatureComponent component = new(_settings, _eventBus);

        component.SetCurrent(5f);
        component.Current.Should().Be(1f);

        component.SetCurrent(-5f);
        component.Current.Should().Be(-1f);
    }
}
