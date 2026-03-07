using FluentAssertions;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Logging;
using MineRPG.Entities.Player;
using MineRPG.Entities.Player.Survival;

using Xunit;

namespace MineRPG.Tests.Entities.Survival;

public sealed class SurvivalSystemTests
{
    private readonly EventBus _eventBus = new(NullLogger.Instance);
    private readonly PlayerMovementSettings _movementSettings = new();
    private readonly SurvivalSettings _survivalSettings = new()
    {
        Health = new HealthSettings
        {
            MaxHealth = 20f,
            NaturalRegenRate = 1f,
            NaturalRegenDelay = 2.5f,
            HungerRegenThreshold = 18f,
            StarveDamageRate = 1f,
            StarveDamageInterval = 4f,
        },
        Hunger = new HungerSettings
        {
            MaxHunger = 20f,
            MaxSaturation = 20f,
            InitialSaturation = 5f,
            PassiveDecayRate = 0.05f,
            SprintDecayRate = 0.1f,
        },
        Thirst = new ThirstSettings
        {
            MaxThirst = 20f,
            PassiveDecayRate = 0.04f,
            SprintDecayRate = 0.08f,
            DehydrationDamageInterval = 5f,
            DehydrationDamageAmount = 1f,
        },
        Stamina = new StaminaSettings
        {
            MaxStamina = 100f,
            SprintDrainRate = 20f,
            RegenRate = 15f,
            RegenDelay = 1.5f,
            MinStaminaToSprint = 10f,
        },
        Breath = new BreathSettings
        {
            MaxBreath = 15f,
            DrainRate = 1f,
            RegenRate = 4f,
            DrowningDamageInterval = 1f,
            DrowningDamageAmount = 2f,
        },
        Temperature = new TemperatureSettings
        {
            ComfortMin = -0.15f,
            ComfortMax = 0.35f,
            AdaptationRate = 0.05f,
            HotDamageInterval = 3f,
            HotDamageAmount = 1f,
            ColdDamageInterval = 3f,
            ColdDamageAmount = 1f,
        },
    };

    private SurvivalSystem CreateSystem(PlayerData playerData)
    {
        return new SurvivalSystem(_survivalSettings, playerData, _eventBus, NullLogger.Instance);
    }

    [Fact]
    public void Tick_DecaysHungerPassively()
    {
        PlayerData playerData = new(_movementSettings);
        SurvivalSystem system = CreateSystem(playerData);
        float initialHunger = system.Hunger.Current;

        // PassiveDecayRate=0.05/s, InitialSaturation=5 → need >100s to exhaust saturation
        system.Tick(200f);

        system.Hunger.Current.Should().BeLessThan(initialHunger);
    }

    [Fact]
    public void Tick_DecaysThirstPassively()
    {
        PlayerData playerData = new(_movementSettings);
        SurvivalSystem system = CreateSystem(playerData);
        float initialThirst = system.Thirst.Current;

        system.Tick(10f);

        system.Thirst.Current.Should().BeLessThan(initialThirst);
    }

    [Fact]
    public void Tick_WhileSprinting_DrainsStamina()
    {
        PlayerData playerData = new(_movementSettings) { IsSprinting = true };
        SurvivalSystem system = CreateSystem(playerData);

        system.Tick(1f);

        system.Stamina.Current.Should().BeLessThan(100f);
    }

    [Fact]
    public void Tick_WhileSprinting_ExhaustsStamina_StopsSprinting()
    {
        PlayerData playerData = new(_movementSettings) { IsSprinting = true };
        SurvivalSystem system = CreateSystem(playerData);

        // 5s at 20/s = 100 stamina drained
        system.Tick(5f);

        playerData.IsSprinting.Should().BeFalse();
    }

    [Fact]
    public void Tick_NotSprinting_RegeneratesStamina()
    {
        PlayerData playerData = new(_movementSettings) { IsSprinting = true };
        SurvivalSystem system = CreateSystem(playerData);

        // Sprint for 2s (drain 40 stamina)
        system.Tick(2f);
        playerData.IsSprinting = false;

        // Wait out regen delay (1.5s) + regen 1s (15/s)
        system.Tick(1.5f);
        system.Tick(1f);

        system.Stamina.Current.Should().BeGreaterThan(60f);
    }

    [Fact]
    public void Tick_Underwater_DrainsBreath()
    {
        PlayerData playerData = new(_movementSettings) { IsUnderwater = true };
        SurvivalSystem system = CreateSystem(playerData);

        system.Tick(5f);

        system.Breath.Current.Should().BeApproximately(10f, 0.01f);
    }

    [Fact]
    public void Tick_Drowning_DamagesHealth()
    {
        PlayerData playerData = new(_movementSettings) { IsUnderwater = true };
        SurvivalSystem system = CreateSystem(playerData);

        // Drain all breath (15s) + 1s drowning = 2 damage
        system.Tick(16f);

        system.Health.Current.Should().BeLessThan(20f);
    }

    [Fact]
    public void ApplyFood_RestoresHunger()
    {
        PlayerData playerData = new(_movementSettings);
        SurvivalSystem system = CreateSystem(playerData);

        // PassiveDecayRate=0.05/s, InitialSaturation=5 → need >100s to exhaust saturation
        system.Tick(200f);
        float before = system.Hunger.Current;

        system.ApplyFood(8f, 12.8f);

        system.Hunger.Current.Should().BeGreaterThan(before);
    }

    [Fact]
    public void ApplyDrink_RestoresThirst()
    {
        PlayerData playerData = new(_movementSettings);
        SurvivalSystem system = CreateSystem(playerData);

        // Decay some thirst
        system.Tick(100f);
        float before = system.Thirst.Current;

        system.ApplyDrink(6f);

        system.Thirst.Current.Should().BeGreaterThan(before);
    }

    [Fact]
    public void Death_PublishesPlayerDiedEvent()
    {
        PlayerData playerData = new(_movementSettings)
        {
            PositionX = 10f,
            PositionY = 65f,
            PositionZ = 20f,
        };
        SurvivalSystem system = CreateSystem(playerData);
        PlayerDiedEvent? received = null;
        _eventBus.Subscribe<PlayerDiedEvent>(e => received = e);

        system.ApplyDamage(25f);
        system.Tick(0.016f);

        received.Should().NotBeNull();
        received!.Value.PositionX.Should().Be(10f);
    }

    [Fact]
    public void Death_DoesNotAutoRespawn()
    {
        PlayerData playerData = new(_movementSettings);
        SurvivalSystem system = CreateSystem(playerData);
        bool respawnReceived = false;
        _eventBus.Subscribe<PlayerRespawnedEvent>(_ => respawnReceived = true);

        system.ApplyDamage(25f);
        system.Tick(0.016f);

        // Dead — should NOT auto-respawn (waits for RespawnRequestedEvent)
        respawnReceived.Should().BeFalse();
    }

    [Fact]
    public void RespawnRequestedEvent_TriggersRespawn()
    {
        PlayerData playerData = new(_movementSettings)
        {
            SpawnX = 8f,
            SpawnY = 80f,
            SpawnZ = 8f,
        };
        SurvivalSystem system = CreateSystem(playerData);
        PlayerRespawnedEvent? received = null;
        _eventBus.Subscribe<PlayerRespawnedEvent>(e => received = e);

        // Kill the player
        system.ApplyDamage(25f);
        system.Tick(0.016f);

        // Request respawn
        _eventBus.Publish(new RespawnRequestedEvent());

        received.Should().NotBeNull();
        received!.Value.SpawnX.Should().Be(8f);
        received.Value.SpawnY.Should().Be(80f);
        system.Health.Current.Should().Be(20f);
    }

    [Fact]
    public void Respawn_ResetsAllVitals()
    {
        PlayerData playerData = new(_movementSettings);
        SurvivalSystem system = CreateSystem(playerData);

        system.ApplyDamage(25f);
        system.Tick(0.016f);
        _eventBus.Publish(new RespawnRequestedEvent());

        system.Health.Current.Should().Be(20f);
        system.Hunger.Current.Should().Be(20f);
        system.Thirst.Current.Should().Be(20f);
        system.Stamina.Current.Should().Be(100f);
        system.Breath.Current.Should().Be(15f);
        system.Temperature.IsComfortable.Should().BeTrue();
    }

    [Fact]
    public void Respawn_TeleportsToSpawn()
    {
        PlayerData playerData = new(_movementSettings)
        {
            PositionX = 100f,
            PositionY = 100f,
            PositionZ = 100f,
            SpawnX = 8f,
            SpawnY = 80f,
            SpawnZ = 8f,
        };
        SurvivalSystem system = CreateSystem(playerData);

        system.ApplyDamage(25f);
        system.Tick(0.016f);
        _eventBus.Publish(new RespawnRequestedEvent());

        playerData.PositionX.Should().Be(8f);
        playerData.PositionY.Should().Be(80f);
        playerData.PositionZ.Should().Be(8f);
    }

    [Fact]
    public void Tick_WhenDead_DoesNothing()
    {
        PlayerData playerData = new(_movementSettings);
        SurvivalSystem system = CreateSystem(playerData);

        system.ApplyDamage(25f);
        system.Tick(0.016f);

        // Player is dead, tick should be no-op
        float hungerBefore = system.Hunger.Current;
        system.Tick(100f);

        // Hunger should not change while dead
        system.Hunger.Current.Should().Be(hungerBefore);
    }

    [Fact]
    public void RespawnRequestedEvent_WhenNotDead_DoesNothing()
    {
        PlayerData playerData = new(_movementSettings);
        SurvivalSystem system = CreateSystem(playerData);
        bool received = false;
        _eventBus.Subscribe<PlayerRespawnedEvent>(_ => received = true);

        // Try to respawn when alive
        _eventBus.Publish(new RespawnRequestedEvent());

        received.Should().BeFalse();
    }
}
