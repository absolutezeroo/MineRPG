using FluentAssertions;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player.Survival;

using Xunit;

namespace MineRPG.Tests.Entities.Survival;

public sealed class HungerComponentTests
{
    private readonly EventBus _eventBus = new(NullLogger.Instance);
    private readonly HungerSettings _settings = new()
    {
        MaxHunger = 20f,
        MaxSaturation = 20f,
        InitialSaturation = 5f,
        PassiveDecayRate = 1f,
        SprintDecayRate = 2f,
    };

    [Fact]
    public void Constructor_StartsAtMaxHungerWithInitialSaturation()
    {
        HungerComponent component = new(_settings, _eventBus);

        component.Current.Should().Be(20f);
        component.Saturation.Should().Be(5f);
        component.Max.Should().Be(20f);
        component.IsStarving.Should().BeFalse();
    }

    [Fact]
    public void Tick_DecaysSaturationFirst()
    {
        HungerComponent component = new(_settings, _eventBus);

        // Passive decay = 1/s * 3s = 3 units. Saturation starts at 5.
        component.Tick(3f);

        component.Saturation.Should().BeApproximately(2f, 0.01f);
        component.Current.Should().Be(20f);
    }

    [Fact]
    public void Tick_DecaysHungerAfterSaturationDepleted()
    {
        HungerComponent component = new(_settings, _eventBus);

        // 5s at 1/s decay: 5 saturation depleted, 0 hunger
        component.Tick(5f);
        component.Saturation.Should().BeApproximately(0f, 0.01f);

        // Another 3s: hunger should drop
        component.Tick(3f);
        component.Current.Should().BeApproximately(17f, 0.01f);
    }

    [Fact]
    public void ApplySprintDecay_AddsExtraDecay()
    {
        HungerComponent component = new(_settings, _eventBus);

        // Deplete saturation first
        component.Tick(5f);

        // Sprint decay = 2/s * 2s = 4 units from hunger
        component.ApplySprintDecay(2f);

        component.Current.Should().BeApproximately(16f, 0.01f);
    }

    [Fact]
    public void RestoreFood_IncreasesHungerAndSaturation()
    {
        HungerComponent component = new(_settings, _eventBus);

        // Deplete some hunger
        component.Tick(5f);
        component.Tick(5f);

        component.RestoreFood(8f, 12.8f);

        component.Current.Should().Be(20f);
        // Saturation capped at hunger level: min(20, 0 + 12.8) = 12.8
        component.Saturation.Should().BeApproximately(12.8f, 0.01f);
    }

    [Fact]
    public void RestoreFood_CapsHungerAtMax()
    {
        HungerComponent component = new(_settings, _eventBus);

        component.RestoreFood(50f, 50f);

        component.Current.Should().Be(20f);
        component.Saturation.Should().Be(20f);
    }

    [Fact]
    public void IsStarving_TrueWhenHungerReachesZero()
    {
        HungerComponent component = new(_settings, _eventBus);

        // Deplete saturation (5s) then deplete hunger (20s)
        component.Tick(25f);

        component.IsStarving.Should().BeTrue();
        component.Current.Should().Be(0f);
    }

    [Fact]
    public void Reset_RestoresToMaxHungerAndInitialSaturation()
    {
        HungerComponent component = new(_settings, _eventBus);
        component.Tick(25f);

        component.Reset();

        component.Current.Should().Be(20f);
        component.Saturation.Should().Be(5f);
    }

    [Fact]
    public void SetValues_ClampsToRange()
    {
        HungerComponent component = new(_settings, _eventBus);

        component.SetValues(10f, 8f);

        component.Current.Should().Be(10f);
        component.Saturation.Should().Be(8f);
    }

    [Fact]
    public void Tick_PublishesHungerChangedEvent()
    {
        HungerComponent component = new(_settings, _eventBus);
        HungerChangedEvent? received = null;
        _eventBus.Subscribe<HungerChangedEvent>(e => received = e);

        // Large enough tick to cross publish threshold
        component.Tick(25f);

        received.Should().NotBeNull();
        received!.Value.Hunger.Should().Be(0f);
        received.Value.MaxHunger.Should().Be(20f);
    }
}
