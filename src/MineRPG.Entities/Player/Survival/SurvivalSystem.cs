using System;
using System.Collections.Generic;

using MineRPG.Core.Events;
using MineRPG.Core.Events.Definitions;
using MineRPG.Core.Interfaces;
using MineRPG.Core.Logging;
using MineRPG.RPG.Inventory;

namespace MineRPG.Entities.Player.Survival;

/// <summary>
/// Orchestrates all six survival components and drives cross-vital interactions.
/// Ticked once per physics frame by the bridge layer.
/// </summary>
public sealed class SurvivalSystem : ITickable
{
    private readonly PlayerData _playerData;
    private readonly IEventBus _eventBus;
    private readonly ILogger _logger;

    private float _starveDamageTimer;
    private bool _isDead;

    /// <summary>
    /// Creates a new survival system with all components.
    /// </summary>
    /// <param name="settings">Survival tuning parameters.</param>
    /// <param name="playerData">Player data for reading sprint and position state.</param>
    /// <param name="eventBus">Event bus for publishing death and respawn events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public SurvivalSystem(
        SurvivalSettings settings,
        PlayerData playerData,
        IEventBus eventBus,
        ILogger logger)
    {
        _playerData = playerData;
        _eventBus = eventBus;
        _logger = logger;

        Health = new HealthComponent(settings.Health, eventBus);
        Hunger = new HungerComponent(settings.Hunger, eventBus);
        Thirst = new ThirstComponent(settings.Thirst, eventBus);
        Stamina = new StaminaComponent(settings.Stamina, eventBus);
        Breath = new BreathComponent(settings.Breath, eventBus);
        Temperature = new TemperatureComponent(settings.Temperature, eventBus);

        Settings = settings;
    }

    /// <summary>The survival settings used by this system.</summary>
    public SurvivalSettings Settings { get; }

    /// <summary>Player health tracking.</summary>
    public HealthComponent Health { get; }

    /// <summary>Player hunger and saturation tracking.</summary>
    public HungerComponent Hunger { get; }

    /// <summary>Player thirst tracking.</summary>
    public ThirstComponent Thirst { get; }

    /// <summary>Player stamina for sprinting.</summary>
    public StaminaComponent Stamina { get; }

    /// <summary>Player breath for underwater survival.</summary>
    public BreathComponent Breath { get; }

    /// <summary>Player body temperature tracking.</summary>
    public TemperatureComponent Temperature { get; }

    /// <summary>
    /// Applies food consumption effects to hunger and saturation.
    /// </summary>
    /// <param name="hungerRestore">Hunger points to restore.</param>
    /// <param name="saturationRestore">Saturation points to restore.</param>
    public void ApplyFood(float hungerRestore, float saturationRestore) => Hunger.RestoreFood(hungerRestore, saturationRestore);

    /// <summary>
    /// Applies drink consumption effects to thirst.
    /// </summary>
    /// <param name="thirstRestore">Thirst points to restore.</param>
    public void ApplyDrink(float thirstRestore) => Thirst.Restore(thirstRestore);

    /// <summary>
    /// Applies healing to the player.
    /// </summary>
    /// <param name="amount">Health points to restore.</param>
    public void ApplyHealing(float amount) => Health.Heal(amount);

    /// <summary>
    /// Applies damage to the player from an external source.
    /// </summary>
    /// <param name="amount">Damage points to apply.</param>
    public void ApplyDamage(float amount) => Health.TakeDamage(amount);

    /// <summary>
    /// Main tick: updates all components in the correct order and handles cross-vital interactions.
    /// </summary>
    /// <param name="deltaTime">Time elapsed since the last tick, in seconds.</param>
    public void Tick(float deltaTime)
    {
        if (_isDead)
        {
            return;
        }

        // 1. Hunger decay (passive)
        Hunger.Tick(deltaTime);

        // 2. Thirst decay (passive)
        Thirst.Tick(deltaTime);

        // 3. Stamina regen (when not sprinting)
        if (!_playerData.IsSprinting)
        {
            Stamina.Tick(deltaTime);
        }

        // 4. Breath drain/regen
        float drowningDamage = Breath.Tick(deltaTime, _playerData.IsUnderwater);

        // 5. Temperature adaptation
        Temperature.NotifyEnvironment(_playerData.CurrentBiomeTemperature, _playerData.PositionY);
        float temperatureDamage = Temperature.Tick(deltaTime);

        // 6. Sprint cross-checks
        if (_playerData.IsSprinting)
        {
            bool hasStamina = Stamina.DrainForSprint(deltaTime);
            Hunger.ApplySprintDecay(deltaTime);
            Thirst.ApplySprintDecay(deltaTime);

            if (!hasStamina)
            {
                _playerData.IsSprinting = false;
            }
        }

        // 7. Environmental damage
        if (drowningDamage > 0f)
        {
            Health.TakeDamage(drowningDamage);
        }

        if (temperatureDamage > 0f)
        {
            Health.TakeDamage(temperatureDamage);
        }

        // 8. Dehydration damage
        float dehydrationDamage = Thirst.TickDehydrationDamage(deltaTime);

        if (dehydrationDamage > 0f)
        {
            Health.TakeDamage(dehydrationDamage);
        }

        // 9. Starvation damage
        if (Hunger.IsStarving)
        {
            _starveDamageTimer += deltaTime;

            if (_starveDamageTimer >= Settings.Health.StarveDamageInterval)
            {
                _starveDamageTimer -= Settings.Health.StarveDamageInterval;
                Health.TakeDamage(Settings.Health.StarveDamageRate);
            }
        }
        else
        {
            _starveDamageTimer = 0f;
        }

        // 10. Health regen (gated by hunger)
        Health.Tick(deltaTime, Hunger.Current);

        // 11. Death check
        if (Health.IsDead)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        _isDead = true;

        _logger.Info(
            "SurvivalSystem: Player died at ({0:F1}, {1:F1}, {2:F1}).",
            _playerData.PositionX, _playerData.PositionY, _playerData.PositionZ);

        _eventBus.Publish(new PlayerDiedEvent
        {
            PositionX = _playerData.PositionX,
            PositionY = _playerData.PositionY,
            PositionZ = _playerData.PositionZ,
        });

        // Drop all inventory items at death position
        DropInventory();

        // Reset all vitals
        Health.Reset();
        Hunger.Reset();
        Thirst.Reset();
        Stamina.Reset();
        Breath.Reset();
        Temperature.Reset();
        _starveDamageTimer = 0f;

        // Teleport to spawn
        _playerData.PositionX = _playerData.SpawnX;
        _playerData.PositionY = _playerData.SpawnY;
        _playerData.PositionZ = _playerData.SpawnZ;
        _playerData.VelocityX = 0f;
        _playerData.VelocityY = 0f;
        _playerData.VelocityZ = 0f;

        _eventBus.Publish(new PlayerRespawnedEvent
        {
            SpawnX = _playerData.SpawnX,
            SpawnY = _playerData.SpawnY,
            SpawnZ = _playerData.SpawnZ,
        });

        _isDead = false;

        _logger.Info(
            "SurvivalSystem: Player respawned at ({0:F1}, {1:F1}, {2:F1}).",
            _playerData.SpawnX, _playerData.SpawnY, _playerData.SpawnZ);
    }

    private void DropInventory()
    {
        PlayerInventory? inventory = _playerData.Inventory;

        if (inventory is null)
        {
            return;
        }

        IReadOnlyList<RPG.Items.ItemInstance> droppedItems = inventory.ClearAll();

        for (int i = 0; i < droppedItems.Count; i++)
        {
            _eventBus.Publish(new ItemDropSpawnedEvent
            {
                ItemDefinitionId = droppedItems[i].DefinitionId,
                Count = droppedItems[i].Count,
                X = _playerData.PositionX,
                Y = _playerData.PositionY + 0.5f,
                Z = _playerData.PositionZ,
            });
        }
    }
}
