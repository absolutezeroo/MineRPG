using FluentAssertions;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player.Survival;

using Xunit;

namespace MineRPG.Tests.Entities.Survival;

public sealed class ThirstComponentTests
{
    private readonly EventBus _eventBus = new(NullLogger.Instance);
    private readonly ThirstSettings _settings = new()
    {
        MaxThirst = 20f,
        PassiveDecayRate = 1f,
        SprintDecayRate = 2f,
        DehydrationDamageInterval = 2f,
        DehydrationDamageAmount = 1f,
    };

    [Fact]
    public void Constructor_StartsAtMaxThirst()
    {
        ThirstComponent component = new(_settings, _eventBus);

        component.Current.Should().Be(20f);
        component.Max.Should().Be(20f);
        component.IsDehydrated.Should().BeFalse();
    }

    [Fact]
    public void Tick_DecaysThirst()
    {
        ThirstComponent component = new(_settings, _eventBus);

        component.Tick(5f);

        component.Current.Should().BeApproximately(15f, 0.01f);
    }

    [Fact]
    public void ApplySprintDecay_AddsExtraDecay()
    {
        ThirstComponent component = new(_settings, _eventBus);

        component.ApplySprintDecay(3f);

        component.Current.Should().BeApproximately(14f, 0.01f);
    }

    [Fact]
    public void Tick_ClampsToZero()
    {
        ThirstComponent component = new(_settings, _eventBus);

        component.Tick(25f);

        component.Current.Should().Be(0f);
        component.IsDehydrated.Should().BeTrue();
    }

    [Fact]
    public void Restore_IncreasesThirst()
    {
        ThirstComponent component = new(_settings, _eventBus);
        component.Tick(10f);

        component.Restore(6f);

        component.Current.Should().BeApproximately(16f, 0.01f);
    }

    [Fact]
    public void Restore_CapsAtMax()
    {
        ThirstComponent component = new(_settings, _eventBus);
        component.Tick(5f);

        component.Restore(50f);

        component.Current.Should().Be(20f);
    }

    [Fact]
    public void Restore_ZeroAmount_DoesNothing()
    {
        ThirstComponent component = new(_settings, _eventBus);
        component.Tick(5f);
        float before = component.Current;

        component.Restore(0f);

        component.Current.Should().Be(before);
    }

    [Fact]
    public void TickDehydrationDamage_WhenNotDehydrated_ReturnsZero()
    {
        ThirstComponent component = new(_settings, _eventBus);

        float damage = component.TickDehydrationDamage(5f);

        damage.Should().Be(0f);
    }

    [Fact]
    public void TickDehydrationDamage_WhenDehydrated_ReturnsDamageAfterInterval()
    {
        ThirstComponent component = new(_settings, _eventBus);
        component.Tick(20f);

        // First 1s — no damage yet (interval is 2s)
        float damage1 = component.TickDehydrationDamage(1f);
        damage1.Should().Be(0f);

        // After 2s total — should trigger
        float damage2 = component.TickDehydrationDamage(1f);
        damage2.Should().Be(1f);
    }

    [Fact]
    public void Reset_RestoresToMax()
    {
        ThirstComponent component = new(_settings, _eventBus);
        component.Tick(20f);

        component.Reset();

        component.Current.Should().Be(20f);
        component.IsDehydrated.Should().BeFalse();
    }

    [Fact]
    public void Tick_PublishesThirstChangedEvent()
    {
        ThirstComponent component = new(_settings, _eventBus);
        ThirstChangedEvent? received = null;
        _eventBus.Subscribe<ThirstChangedEvent>(e => received = e);

        component.Tick(20f);

        received.Should().NotBeNull();
        received!.Value.NewValue.Should().Be(0f);
        received.Value.MaxValue.Should().Be(20f);
    }
}
