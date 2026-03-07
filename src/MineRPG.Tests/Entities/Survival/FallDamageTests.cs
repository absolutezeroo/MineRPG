using FluentAssertions;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Entities.Player.Survival;

using Xunit;

namespace MineRPG.Tests.Entities.Survival;

public sealed class FallDamageTests
{
    private readonly EventBus _eventBus = new(NullLogger.Instance);
    private readonly PlayerMovementSettings _movementSettings = new();
    private readonly SurvivalSettings _survivalSettings = new()
    {
        Health = new HealthSettings
        {
            MaxHealth = 20f,
            NaturalRegenRate = 0f,
            NaturalRegenDelay = 999f,
            HungerRegenThreshold = 18f,
            StarveDamageRate = 1f,
            StarveDamageInterval = 999f,
        },
        FallDamage = new FallDamageSettings
        {
            SafeFallDistance = 3.0f,
            DamagePerBlock = 1.0f,
            MaxFallDamage = 200.0f,
            WaterNegatesFallDamage = true,
        },
    };

    private SurvivalSystem CreateSystem(PlayerData playerData)
    {
        return new SurvivalSystem(_survivalSettings, playerData, _eventBus, NullLogger.Instance);
    }

    [Fact]
    public void Tick_FallBelowSafeDistance_NoDamage()
    {
        PlayerData playerData = new(_movementSettings) { PendingFallDistance = 2.0f };
        SurvivalSystem system = CreateSystem(playerData);

        system.Tick(0.016f);

        system.Health.Current.Should().Be(20f);
    }

    [Fact]
    public void Tick_FallExactlySafeDistance_NoDamage()
    {
        PlayerData playerData = new(_movementSettings) { PendingFallDistance = 3.0f };
        SurvivalSystem system = CreateSystem(playerData);

        system.Tick(0.016f);

        system.Health.Current.Should().Be(20f);
    }

    [Fact]
    public void Tick_FallAboveSafeDistance_CorrectDamage()
    {
        PlayerData playerData = new(_movementSettings) { PendingFallDistance = 7.0f };
        SurvivalSystem system = CreateSystem(playerData);

        system.Tick(0.016f);

        // 7 - 3 = 4 blocks beyond safe distance, 4 * 1.0 = 4 damage
        system.Health.Current.Should().Be(16f);
    }

    [Fact]
    public void Tick_FallDamage_CappedAtMaxFallDamage()
    {
        SurvivalSettings settings = new()
        {
            Health = new HealthSettings
            {
                MaxHealth = 500f,
                NaturalRegenRate = 0f,
                NaturalRegenDelay = 999f,
                HungerRegenThreshold = 18f,
                StarveDamageRate = 1f,
                StarveDamageInterval = 999f,
            },
            FallDamage = new FallDamageSettings
            {
                SafeFallDistance = 3.0f,
                DamagePerBlock = 1.0f,
                MaxFallDamage = 200.0f,
                WaterNegatesFallDamage = true,
            },
        };

        PlayerData playerData = new(_movementSettings) { PendingFallDistance = 300.0f };
        SurvivalSystem system = new(settings, playerData, _eventBus, NullLogger.Instance);

        system.Tick(0.016f);

        // 300 - 3 = 297 * 1.0 = 297, capped at 200
        system.Health.Current.Should().Be(300f);
    }

    [Fact]
    public void Tick_FallIntoWater_NoDamage()
    {
        PlayerData playerData = new(_movementSettings)
        {
            PendingFallDistance = 20.0f,
            IsUnderwater = true,
        };
        SurvivalSystem system = CreateSystem(playerData);

        system.Tick(0.016f);

        system.Health.Current.Should().Be(20f);
    }

    [Fact]
    public void Tick_NegativePendingFallDistance_Ignored()
    {
        PlayerData playerData = new(_movementSettings) { PendingFallDistance = -5.0f };
        SurvivalSystem system = CreateSystem(playerData);

        system.Tick(0.016f);

        system.Health.Current.Should().Be(20f);
        playerData.PendingFallDistance.Should().Be(-5.0f);
    }

    [Fact]
    public void Tick_FallDamage_ClearsPendingFallDistance()
    {
        PlayerData playerData = new(_movementSettings) { PendingFallDistance = 10.0f };
        SurvivalSystem system = CreateSystem(playerData);

        system.Tick(0.016f);

        playerData.PendingFallDistance.Should().Be(0f);
    }

    [Fact]
    public void Tick_FallDamage_PublishesPlayerLandedEvent()
    {
        PlayerData playerData = new(_movementSettings) { PendingFallDistance = 7.0f };
        SurvivalSystem system = CreateSystem(playerData);
        PlayerLandedEvent? received = null;
        _eventBus.Subscribe<PlayerLandedEvent>(e => received = e);

        system.Tick(0.016f);

        received.Should().NotBeNull();
        received!.Value.FallDistance.Should().Be(7.0f);
        received.Value.DamageTaken.Should().Be(4.0f);
    }

    [Fact]
    public void Tick_SafeFall_PublishesPlayerLandedEventWithZeroDamage()
    {
        PlayerData playerData = new(_movementSettings) { PendingFallDistance = 2.0f };
        SurvivalSystem system = CreateSystem(playerData);
        PlayerLandedEvent? received = null;
        _eventBus.Subscribe<PlayerLandedEvent>(e => received = e);

        system.Tick(0.016f);

        received.Should().NotBeNull();
        received!.Value.FallDistance.Should().Be(2.0f);
        received.Value.DamageTaken.Should().Be(0f);
    }

    [Fact]
    public void Tick_LethalFallDamage_PublishesDeath()
    {
        PlayerData playerData = new(_movementSettings) { PendingFallDistance = 23.0f };
        SurvivalSystem system = CreateSystem(playerData);
        PlayerDiedEvent? died = null;
        _eventBus.Subscribe<PlayerDiedEvent>(e => died = e);

        system.Tick(0.016f);

        // 23 - 3 = 20 damage, exactly lethal (20 HP)
        system.Health.Current.Should().Be(0f);
        died.Should().NotBeNull();
    }

    [Fact]
    public void Tick_WaterNegatesFallDamageDisabled_StillTakesDamage()
    {
        SurvivalSettings settings = new()
        {
            Health = new HealthSettings
            {
                MaxHealth = 20f,
                NaturalRegenRate = 0f,
                NaturalRegenDelay = 999f,
                HungerRegenThreshold = 18f,
                StarveDamageRate = 1f,
                StarveDamageInterval = 999f,
            },
            FallDamage = new FallDamageSettings
            {
                SafeFallDistance = 3.0f,
                DamagePerBlock = 1.0f,
                MaxFallDamage = 200.0f,
                WaterNegatesFallDamage = false,
            },
        };

        PlayerData playerData = new(_movementSettings)
        {
            PendingFallDistance = 10.0f,
            IsUnderwater = true,
        };
        SurvivalSystem system = new(settings, playerData, _eventBus, NullLogger.Instance);

        system.Tick(0.016f);

        // Water does NOT negate when setting is disabled
        system.Health.Current.Should().Be(13f);
    }
}
