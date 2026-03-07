using FluentAssertions;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player.Survival;

using Xunit;

namespace MineRPG.Tests.Entities.Survival;

public sealed class HealthComponentTests
{
    private readonly EventBus _eventBus = new(NullLogger.Instance);
    private readonly HealthSettings _settings = new()
    {
        MaxHealth = 20f,
        NaturalRegenRate = 1f,
        NaturalRegenDelay = 2.5f,
        HungerRegenThreshold = 18f,
        StarveDamageRate = 1f,
        StarveDamageInterval = 4f,
    };

    [Fact]
    public void Constructor_StartsAtMaxHealth()
    {
        HealthComponent component = new(_settings, _eventBus);

        component.Current.Should().Be(20f);
        component.Max.Should().Be(20f);
        component.IsDead.Should().BeFalse();
    }

    [Fact]
    public void TakeDamage_ReducesHealth()
    {
        HealthComponent component = new(_settings, _eventBus);

        component.TakeDamage(5f);

        component.Current.Should().Be(15f);
    }

    [Fact]
    public void TakeDamage_ClampsToZero()
    {
        HealthComponent component = new(_settings, _eventBus);

        component.TakeDamage(25f);

        component.Current.Should().Be(0f);
        component.IsDead.Should().BeTrue();
    }

    [Fact]
    public void TakeDamage_ZeroAmount_DoesNothing()
    {
        HealthComponent component = new(_settings, _eventBus);

        component.TakeDamage(0f);

        component.Current.Should().Be(20f);
    }

    [Fact]
    public void TakeDamage_NegativeAmount_DoesNothing()
    {
        HealthComponent component = new(_settings, _eventBus);

        component.TakeDamage(-5f);

        component.Current.Should().Be(20f);
    }

    [Fact]
    public void Heal_RestoresHealth()
    {
        HealthComponent component = new(_settings, _eventBus);
        component.TakeDamage(10f);

        component.Heal(5f);

        component.Current.Should().Be(15f);
    }

    [Fact]
    public void Heal_ClampsToMax()
    {
        HealthComponent component = new(_settings, _eventBus);
        component.TakeDamage(3f);

        component.Heal(10f);

        component.Current.Should().Be(20f);
    }

    [Fact]
    public void Tick_DuringRegenDelay_DoesNotRegen()
    {
        HealthComponent component = new(_settings, _eventBus);
        component.TakeDamage(5f);

        // Regen delay is 2.5s, tick only 1s
        component.Tick(1f, 20f);

        component.Current.Should().Be(15f);
    }

    [Fact]
    public void Tick_AfterRegenDelay_RegeneratesHealth()
    {
        HealthComponent component = new(_settings, _eventBus);
        component.TakeDamage(5f);

        // Wait out regen delay (2.5s) then regen (1 hp/s for 1s)
        component.Tick(2.5f, 20f);
        component.Tick(1f, 20f);

        component.Current.Should().BeApproximately(16f, 0.01f);
    }

    [Fact]
    public void Tick_LowHunger_DoesNotRegen()
    {
        HealthComponent component = new(_settings, _eventBus);
        component.TakeDamage(5f);

        // Wait out regen delay then tick with low hunger (below threshold of 18)
        component.Tick(3f, 17f);
        component.Tick(1f, 17f);

        component.Current.Should().Be(15f);
    }

    [Fact]
    public void TakeDamage_ResetsRegenDelay()
    {
        HealthComponent component = new(_settings, _eventBus);
        component.TakeDamage(5f);

        // Wait 2s into the regen delay
        component.Tick(2f, 20f);

        // Take more damage — resets the delay
        component.TakeDamage(1f);

        // 1s later — should still be in delay
        component.Tick(1f, 20f);

        component.Current.Should().Be(14f);
    }

    [Fact]
    public void Reset_RestoresToMax()
    {
        HealthComponent component = new(_settings, _eventBus);
        component.TakeDamage(15f);

        component.Reset();

        component.Current.Should().Be(20f);
        component.IsDead.Should().BeFalse();
    }

    [Fact]
    public void SetCurrent_ClampsToRange()
    {
        HealthComponent component = new(_settings, _eventBus);

        component.SetCurrent(50f);
        component.Current.Should().Be(20f);

        component.SetCurrent(-5f);
        component.Current.Should().Be(0f);
    }

    [Fact]
    public void TakeDamage_PublishesHealthChangedEvent()
    {
        HealthComponent component = new(_settings, _eventBus);
        HealthChangedEvent? received = null;
        _eventBus.Subscribe<HealthChangedEvent>(e => received = e);

        component.TakeDamage(5f);

        received.Should().NotBeNull();
        received!.Value.NewValue.Should().Be(15f);
        received.Value.MaxValue.Should().Be(20f);
    }
}
