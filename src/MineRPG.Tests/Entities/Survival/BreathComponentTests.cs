using FluentAssertions;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player.Survival;

using Xunit;

namespace MineRPG.Tests.Entities.Survival;

public sealed class BreathComponentTests
{
    private readonly EventBus _eventBus = new(NullLogger.Instance);
    private readonly BreathSettings _settings = new()
    {
        MaxBreath = 15f,
        DrainRate = 1f,
        RegenRate = 4f,
        DrowningDamageInterval = 1f,
        DrowningDamageAmount = 2f,
    };

    [Fact]
    public void Constructor_StartsAtMaxBreath()
    {
        BreathComponent component = new(_settings, _eventBus);

        component.Current.Should().Be(15f);
        component.Max.Should().Be(15f);
        component.IsDrowning.Should().BeFalse();
    }

    [Fact]
    public void Tick_Underwater_DrainsBreath()
    {
        BreathComponent component = new(_settings, _eventBus);

        float damage = component.Tick(5f, true);

        component.Current.Should().BeApproximately(10f, 0.01f);
        damage.Should().Be(0f);
    }

    [Fact]
    public void Tick_Underwater_ClampsToZero()
    {
        BreathComponent component = new(_settings, _eventBus);

        component.Tick(15f, true);

        component.Current.Should().Be(0f);
        component.IsDrowning.Should().BeTrue();
    }

    [Fact]
    public void Tick_AboveSurface_RegensBreath()
    {
        BreathComponent component = new(_settings, _eventBus);
        component.Tick(10f, true);

        component.Tick(1f, false);

        component.Current.Should().BeApproximately(9f, 0.01f);
    }

    [Fact]
    public void Tick_AboveSurface_ClampsToMax()
    {
        BreathComponent component = new(_settings, _eventBus);
        component.Tick(5f, true);

        component.Tick(10f, false);

        component.Current.Should().Be(15f);
    }

    [Fact]
    public void Tick_Drowning_ReturnsDamageAfterInterval()
    {
        BreathComponent component = new(_settings, _eventBus);

        // Drain all breath (15s at 1/s)
        component.Tick(15f, true);

        // Drowning: 1s interval → should take damage
        float damage = component.Tick(1f, true);

        damage.Should().Be(2f);
    }

    [Fact]
    public void Tick_Drowning_NoDamageBeforeInterval()
    {
        BreathSettings shortBreath = new()
        {
            MaxBreath = 1f,
            DrainRate = 2f,
            RegenRate = 4f,
            DrowningDamageInterval = 1f,
            DrowningDamageAmount = 2f,
        };
        BreathComponent component = new(shortBreath, _eventBus);

        // Drain all breath quickly (0.5s at 2/s = 1 unit drained, breath = 0)
        // Drowning timer accumulates 0.5s (the portion of the tick after breath hit 0)
        component.Tick(0.5f, true);
        component.IsDrowning.Should().BeTrue();

        // 0.3s more: timer = 0.5 + 0.3 = 0.8s < 1.0s interval → no damage
        float damage = component.Tick(0.3f, true);

        damage.Should().Be(0f);
    }

    [Fact]
    public void Tick_PublishesStartedDrowningEvent()
    {
        BreathComponent component = new(_settings, _eventBus);
        bool received = false;
        _eventBus.Subscribe<PlayerStartedDrowningEvent>(_ => received = true);

        component.Tick(15f, true);

        received.Should().BeTrue();
    }

    [Fact]
    public void Tick_PublishesStoppedDrowningEvent()
    {
        BreathComponent component = new(_settings, _eventBus);
        component.Tick(15f, true);

        bool received = false;
        _eventBus.Subscribe<PlayerStoppedDrowningEvent>(_ => received = true);

        component.Tick(1f, false);

        received.Should().BeTrue();
    }

    [Fact]
    public void Reset_RestoresToMaxAndClearsDrowning()
    {
        BreathComponent component = new(_settings, _eventBus);
        component.Tick(15f, true);

        component.Reset();

        component.Current.Should().Be(15f);
        component.IsDrowning.Should().BeFalse();
    }

    [Fact]
    public void Tick_PublishesBreathChangedEvent()
    {
        BreathComponent component = new(_settings, _eventBus);
        BreathChangedEvent? received = null;
        _eventBus.Subscribe<BreathChangedEvent>(e => received = e);

        component.Tick(15f, true);

        received.Should().NotBeNull();
        received!.Value.NewValue.Should().Be(0f);
        received.Value.MaxValue.Should().Be(15f);
    }
}
