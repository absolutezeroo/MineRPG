using FluentAssertions;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player.Survival;

using Xunit;

namespace MineRPG.Tests.Entities.Survival;

public sealed class StaminaComponentTests
{
    private readonly EventBus _eventBus = new(NullLogger.Instance);
    private readonly StaminaSettings _settings = new()
    {
        MaxStamina = 100f,
        SprintDrainRate = 20f,
        RegenRate = 15f,
        RegenDelay = 1.5f,
        MinStaminaToSprint = 10f,
    };

    [Fact]
    public void Constructor_StartsAtMaxStamina()
    {
        StaminaComponent component = new(_settings, _eventBus);

        component.Current.Should().Be(100f);
        component.Max.Should().Be(100f);
        component.CanSprint.Should().BeTrue();
    }

    [Fact]
    public void DrainForSprint_ReducesStamina()
    {
        StaminaComponent component = new(_settings, _eventBus);

        bool result = component.DrainForSprint(1f);

        component.Current.Should().BeApproximately(80f, 0.01f);
        result.Should().BeTrue();
    }

    [Fact]
    public void DrainForSprint_ReturnsFalseWhenExhausted()
    {
        StaminaComponent component = new(_settings, _eventBus);

        // Drain 5s at 20/s = 100
        bool result = component.DrainForSprint(5f);

        component.Current.Should().Be(0f);
        result.Should().BeFalse();
    }

    [Fact]
    public void CanSprint_FalseWhenBelowThreshold()
    {
        StaminaComponent component = new(_settings, _eventBus);

        // Drain to 5 (below threshold of 10)
        component.DrainForSprint(4.75f);

        component.CanSprint.Should().BeFalse();
    }

    [Fact]
    public void Tick_DuringRegenDelay_DoesNotRegen()
    {
        StaminaComponent component = new(_settings, _eventBus);
        component.DrainForSprint(2f);

        // 1s into the 1.5s regen delay
        component.Tick(1f);

        component.Current.Should().BeApproximately(60f, 0.01f);
    }

    [Fact]
    public void Tick_AfterRegenDelay_RegeneratesStamina()
    {
        StaminaComponent component = new(_settings, _eventBus);
        component.DrainForSprint(2f);

        // Exhaust regen delay (1.5s) then regen (15/s for 1s)
        component.Tick(1.5f);
        component.Tick(1f);

        component.Current.Should().BeApproximately(75f, 0.01f);
    }

    [Fact]
    public void Tick_ClampsToMax()
    {
        StaminaComponent component = new(_settings, _eventBus);
        component.DrainForSprint(0.5f);

        // Exhaust delay and regen fully
        component.Tick(1.5f);
        component.Tick(10f);

        component.Current.Should().Be(100f);
    }

    [Fact]
    public void Reset_RestoresToMax()
    {
        StaminaComponent component = new(_settings, _eventBus);
        component.DrainForSprint(5f);

        component.Reset();

        component.Current.Should().Be(100f);
        component.CanSprint.Should().BeTrue();
    }

    [Fact]
    public void DrainForSprint_PublishesStaminaChangedEvent()
    {
        StaminaComponent component = new(_settings, _eventBus);
        StaminaChangedEvent? received = null;
        _eventBus.Subscribe<StaminaChangedEvent>(e => received = e);

        component.DrainForSprint(5f);

        received.Should().NotBeNull();
        received!.Value.NewValue.Should().Be(0f);
        received.Value.MaxValue.Should().Be(100f);
    }
}
